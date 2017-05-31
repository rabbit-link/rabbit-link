#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Configuration;
using RabbitLink.Connection;
using RabbitLink.Exceptions;
using RabbitLink.Internals.Queues;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Producer
{
    internal class LinkProducer : ILinkProducerIntenal, ILinkChannelHandler
    {
        #region Fields

        private readonly ILinkChannel _channel;
        private readonly LinkProducerConfiguration _configuration;

        private readonly LinkConfiguration _linkConfiguration;
        private readonly ILinkLogger _logger;

        private readonly WorkQueue<LinkProducerMessage> _messageQueue =
            new WorkQueue<LinkProducerMessage>();

        private readonly object _sync = new object();

        private readonly Func<Exception, Task> _topologyConfigErrorHandler;
        private readonly LinkTopologyRunner<ILinkExchage> _topologyRunner;

        private ILinkExchage _exchage;

        #endregion

        #region Ctor

        public LinkProducer(LinkProducerConfiguration configuration, LinkConfiguration linkConfiguration,
            ILinkChannel channel,
            Func<ILinkTopologyConfig, Task<ILinkExchage>> topologyConfigHandler,
            Func<Exception, Task> topologyConfigErrorHandler)
        {
            _linkConfiguration = linkConfiguration ?? throw new ArgumentNullException(nameof(linkConfiguration));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            if (topologyConfigHandler == null)
                throw new ArgumentNullException(nameof(topologyConfigHandler));

            _topologyConfigErrorHandler = topologyConfigErrorHandler ??
                                          throw new ArgumentNullException(nameof(topologyConfigErrorHandler));

            _channel = channel ?? throw new ArgumentNullException(nameof(channel));

            _logger = linkConfiguration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");
            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(linkConfiguration.LoggerFactory));

            _topologyRunner =
                new LinkTopologyRunner<ILinkExchage>(_logger, _linkConfiguration.UseThreads, topologyConfigHandler);
            _channel.Disposed += ChannelOnDisposed;

            _logger.Debug($"Created(channelId: {_channel.Id})");

            _channel.Initialize(this);
        }

        #endregion

        #region ILinkChannelHandler Members

        public async Task OnActive(IModel model, CancellationToken cancellation)
        {
            var newState = State;

            while (true)
            {
                if (cancellation.IsCancellationRequested)
                {
                    newState = LinkProducerState.Stop;
                }

                if (newState != State)
                {
                    _logger.Debug($"State change {State} -> {newState}");
                    State = newState;
                }

                switch (State)
                {
                    case LinkProducerState.Init:
                        newState = LinkProducerState.Configure;
                        break;
                    case LinkProducerState.Configure:
                    case LinkProducerState.Reconfigure:
                        newState = await OnConfigureAsync(model, State == LinkProducerState.Reconfigure,
                                cancellation)
                            .ConfigureAwait(false);
                        break;
                    default:
                        throw new NotImplementedException($"Handler for state ${State} not implemeted");
                }
            }
        }

        public Task OnConnecting(CancellationToken cancellation)
        {
            return _messageQueue.YieldAsync(cancellation);
        }

        public void MessageAck(BasicAckEventArgs info)
        {
            throw new NotImplementedException();
        }

        public void MessageNack(BasicNackEventArgs info)
        {
            throw new NotImplementedException();
        }

        public void MessageReturn(BasicReturnEventArgs info)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ILinkProducerIntenal Members

        public event EventHandler Disposed;

        public void Dispose()
        {
            Dispose(false);
        }

        public LinkProducerState State { get; private set; } = LinkProducerState.Init;

        public Task PublishAsync<T>(T body, LinkMessageProperties properties = null,
            LinkPublishProperties publishProperties = null,
            CancellationToken? cancellation = null) where T : class
        {
            var msgProperties = _configuration.MessageProperties.Clone();
            if (properties != null)
            {
                msgProperties.CopyFrom(properties);
            }

            if (typeof(T) == typeof(byte[]))
            {
                return PublishRawAsync(body as byte[], msgProperties, publishProperties, cancellation);
            }

            byte[] rawBody;
            try
            {
                rawBody = _configuration.MessageSerializer.Serialize(body, msgProperties);
            }
            catch (Exception ex)
            {
                throw new LinkSerializationException(ex);
            }

            var typeName = _configuration.TypeNameMapping.Map<T>();
            if (typeName != null)
            {
                msgProperties.Type = typeName;
            }

            return PublishRawAsync(rawBody, msgProperties, publishProperties, cancellation);
        }

        public Guid Id { get; } = Guid.NewGuid();

        public bool ConfirmsMode => _configuration.ConfirmsMode;

        public LinkPublishProperties PublishProperties => _configuration.PublishProperties.Clone();
        public LinkMessageProperties MessageProperties => _configuration.MessageProperties.Clone();
        public TimeSpan? PublishTimeout => _configuration.PublishTimeout;

        #endregion

        private async Task<LinkProducerState> OnConfigureAsync(IModel model, bool retry, CancellationToken cancellation)
        {
            if (retry)
            {
                try
                {
                    _logger.Info($"Retrying in {_linkConfiguration.TopologyRecoveryInterval.TotalSeconds:0.###}s");
                    await Task.Delay(_linkConfiguration.TopologyRecoveryInterval, cancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return LinkProducerState.Reconfigure;
                }
            }

            _logger.Info("Configuring topology");

            try
            {
                _exchage = await _topologyRunner
                    .RunAsync(model, cancellation)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Exception on topology configuration: {ex}");

                try
                {
                    await _topologyConfigErrorHandler(ex)
                        .ConfigureAwait(false);
                }
                catch (Exception handlerException)
                {
                    _logger.Error($"Exception in topology error handler: {handlerException}");
                }

                return LinkProducerState.Reconfigure;
            }

            _logger.Info("Topology configured");

            return LinkProducerState.Active;
        }

        private void ChannelOnDisposed(object sender, EventArgs eventArgs)
        {
            _logger.Debug("Channel disposed, disposing...");
            Dispose(true);
        }

        private void Dispose(bool byChannel)
        {
            if (State == LinkProducerState.Dispose)
                return;

            lock (_sync)
            {
                if (State == LinkProducerState.Dispose)
                    return;

                _logger.Debug("Disposing");

                _channel.Disposed -= ChannelOnDisposed;
                if (!byChannel)
                {
                    _channel.Dispose();
                }

                State = LinkProducerState.Dispose;

                var ex = new ObjectDisposedException(GetType().Name);
                _messageQueue.Complete(msg => msg.TrySetException(ex));

                _logger.Debug("Disposed");
                _logger.Dispose();

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        private Task PublishRawAsync(byte[] body, LinkMessageProperties properties,
            LinkPublishProperties publishProperties = null,
            CancellationToken? cancellation = null)
        {
            if (State == LinkProducerState.Dispose)
                throw new ObjectDisposedException(GetType().Name);

            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            if (cancellation == null)
            {
                cancellation = _configuration.PublishTimeout != null
                    ? new CancellationTokenSource(_configuration.PublishTimeout.Value).Token
                    : CancellationToken.None;
            }

            var msgPublishProperties = _configuration.PublishProperties.Clone();
            if (publishProperties != null)
            {
                msgPublishProperties.Extend(publishProperties);
            }

            var msgProperties = properties.Clone();
            msgProperties.AppId = _linkConfiguration.AppId;

            if (_configuration.SetUserId)
            {
                msgProperties.UserId = _channel.Connection.UserId;
            }

            _configuration.MessageIdGenerator.SetMessageId(body, msgProperties, msgPublishProperties);
            var msg = new LinkProducerMessage(body, msgProperties, msgPublishProperties, cancellation.Value);

            try
            {
                _messageQueue.Put(msg);
            }
            catch
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return msg.Completion;
        }
    }
}