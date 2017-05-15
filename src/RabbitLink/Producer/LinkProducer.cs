#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Async;
using RabbitLink.Configuration;
using RabbitLink.Connection;
using RabbitLink.Exceptions;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Producer
{
    internal class LinkProducer : ILinkProducerIntenal
    {
        #region .ctor

        public LinkProducer(LinkProducerConfiguration configuration, LinkConfiguration linkConfiguration,
            ILinkChannel channel,
            Func<ILinkTopologyConfig, Task<ILinkExchage>> topologyConfigHandler,
            Func<Exception, Task> topologyConfigErrorHandler)
        {
            if (linkConfiguration == null)
                throw new ArgumentNullException(nameof(linkConfiguration));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            if (topologyConfigHandler == null)
                throw new ArgumentNullException(nameof(topologyConfigHandler));

            if (topologyConfigErrorHandler == null)
                throw new ArgumentNullException(nameof(topologyConfigErrorHandler));

            _configuration = configuration;
            _linkConfiguration = linkConfiguration;
            _logger = linkConfiguration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(linkConfiguration.LoggerFactory));

            _topologyConfigHandler = topologyConfigHandler;
            _topologyConfigErrorHandler = topologyConfigErrorHandler;

            _disposedCancellationSource = new CancellationTokenSource();
            _disposedCancellation = _disposedCancellationSource.Token;

            _channel = channel;
            _channel.Ack += ChannelOnAck;
            _channel.Nack += ChannelOnNack;
            _channel.Return += ChannelOnReturn;

            _topology = new LinkTopology(linkConfiguration, _channel,
                new LinkActionsTopologyHandler(TopologyConfigureAsync, TopologyReadyAsync, TopologyConfigurationErrorAsync), false);
            _topology.Disposed += TopologyOnDisposed;

            _logger.Debug($"Created(channelId: {_channel.Id})");
        }

        #endregion

        #region Events

        public event EventHandler Disposed;

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            Dispose(false);
        }

        private void Dispose(bool byTopology)
        {
            if (_disposedCancellation.IsCancellationRequested) return;

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested) return;

                _logger.Debug("Disposing");

                _disposedCancellationSource.Cancel();
                _disposedCancellationSource.Dispose();

                _topology.Disposed -= TopologyOnDisposed;

                if (!byTopology)
                {
                    _topology.Dispose();
                }

                _loopCancellationSource?.Cancel();
                _loopCancellationSource?.Dispose();

                // ReSharper disable once MethodSupportsCancellation
                _loopTask?.WaitAndUnwrapException();                

                // cancelling requests
                var ex = new ObjectDisposedException(GetType().Name);

                using (_ackQueueLock.Lock())
                {
                    while (_ackQueue.Last != null)
                    {
                        _ackQueue.Last.Value.SetException(ex);
                        _ackQueue.RemoveLast();
                    }
                }

                _messageQueue.Dispose();

                _logger.Debug("Disposed");
                _logger.Dispose();
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public methods        

        public Task PublishAsync<T>(T body, LinkMessageProperties properties = null, LinkPublishProperties publishProperties = null,
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

        #endregion

        #region Private methods

        private async Task PublishRawAsync(byte[] body, LinkMessageProperties properties, LinkPublishProperties publishProperties = null,
            CancellationToken? cancellation = null)
        {
            if (_disposedCancellation.IsCancellationRequested)
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
            var msg = new LinkProducerQueueMessage(body, msgProperties, msgPublishProperties, cancellation.Value);

            try
            {
                await _messageQueue.EnqueueAsync(msg)
                    .ConfigureAwait(false);
            }
            catch
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            await msg.Task
                .ConfigureAwait(false);
        }

        #endregion        

        #region Fields

        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly CancellationToken _disposedCancellation;

        private readonly LinkedList<LinkProducerQueueMessage> _ackQueue = new LinkedList<LinkProducerQueueMessage>();
        private readonly AsyncLock _ackQueueLock = new AsyncLock();

        private readonly LinkProducerQueue _messageQueue = new LinkProducerQueue();

        private readonly Func<ILinkTopologyConfig, Task<ILinkExchage>> _topologyConfigHandler;
        private readonly Func<Exception, Task> _topologyConfigErrorHandler;

        private Task _loopTask;
        private CancellationTokenSource _loopCancellationSource;
        private CancellationToken _loopCancellation;

        private readonly object _sync = new object();

        private readonly LinkProducerConfiguration _configuration;
        private readonly LinkConfiguration _linkConfiguration;

        private readonly ILinkChannel _channel;
        private readonly ILinkTopology _topology;
        private readonly ILinkLogger _logger;
        private ILinkExchage _exchage;

        #endregion

        #region Properties

        public Guid Id { get; } = Guid.NewGuid();

        public bool ConfirmsMode => _configuration.ConfirmsMode;

        public LinkPublishProperties PublishProperties => _configuration.PublishProperties.Clone();
        public LinkMessageProperties MessageProperties => _configuration.MessageProperties.Clone();
        public TimeSpan? PublishTimeout => _configuration.PublishTimeout;

        #endregion

        #region Send

        private async Task RequeueUnackedAsync()
        {            
            using (await _ackQueueLock.LockAsync().ConfigureAwait(false))
            {
                if (!_ackQueue.Any())
                    return;

                _logger.Warning($"Requeuing {_ackQueue.Count} not ACKed or NACKed messages");
                await _messageQueue.EnqueueRetryAsync(_ackQueue, true)
                    .ConfigureAwait(false);
            }
        }

        private async Task SendMessageAsync(LinkProducerQueueMessage msg, CancellationToken cancellation)
        {
            await _channel.InvokeActionAsync(model =>
            {
                using (_ackQueueLock.Lock())
                {
                    msg.Sequence = model.NextPublishSeqNo;

                    var properties = model.CreateBasicProperties();
                    msg.Properties.CopyTo(properties);

                    model.BasicPublish(_exchage.Name, msg.PublishProperties.RoutingKey ?? "",
                        msg.PublishProperties.Mandatory ?? false, properties,
                        msg.Body);

                    if (ConfirmsMode)
                    {
                        _ackQueue.AddFirst(msg);
                    }
                }
            }, cancellation)
                .ConfigureAwait(false);
        }

        private async Task SendPublishQueueAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                LinkProducerQueueMessage msg;

                try
                {
                    msg = await _messageQueue.DequeueAsync(_loopCancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                try
                {
                    using (var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation, msg.Cancellation))
                    {
                        await SendMessageAsync(msg, compositeCancellation.Token)
                            .ConfigureAwait(false);
                    }

                    if (!ConfirmsMode)
                    {
                        msg.SetResult();
                    }
                }
                catch (Exception ex)
                {
                    if (msg.Cancellation.IsCancellationRequested)
                    {
                        // Message cancelled, looping for the next                        
                        msg.SetCancelled();
                        continue;
                    }

                    // Error on publish
                    _logger.Error($"Cannot publish message: {ex.Message}");
                    _logger.Debug("Queuing message for retry");
                    await _messageQueue.EnqueueRetryAsync(msg)
                        .ConfigureAwait(false);

                    return;
                }
            }
        }

        private async Task SendLoopAsync()
        {
            if (ConfirmsMode)
            {
                await _channel.InvokeActionAsync(model => model.ConfirmSelect(), _loopCancellation)
                    .ConfigureAwait(false);
            }

            await SendPublishQueueAsync(_loopCancellation).ConfigureAwait(false);

            if (!_disposedCancellation.IsCancellationRequested)
            {
                await RequeueUnackedAsync().ConfigureAwait(false);

                if (!_loopCancellation.IsCancellationRequested && _channel.IsOpen)
                {
                    _logger.Info("Channel is open and publish loop ends, scheduling reconfiguration.");
                    _topology.ScheduleConfiguration(true);
                }
            }            
        }

        #endregion

        #region Channel handlers

        private void ChannelOnReturn(object sender, BasicReturnEventArgs e)
        {
            using (_ackQueueLock.Lock())
            {
                if (_ackQueue.Last != null)
                {
                    var msg = _ackQueue.Last.Value;
                    _ackQueue.RemoveLast();
                    msg.SetException(new LinkMessageReturnedException(e.ReplyText));
                }
            }
        }

        private void ChannelOnNack(object sender, BasicNackEventArgs e)
        {
            using (_ackQueueLock.Lock())
            {
                while (_ackQueue.Last != null && _ackQueue.Last.Value.Sequence <= e.DeliveryTag)
                {
                    var msg = _ackQueue.Last.Value;
                    _ackQueue.RemoveLast();
                    msg.SetException(new LinkMessageNackedException());
                }
            }
        }

        private void ChannelOnAck(object sender, BasicAckEventArgs e)
        {
            using (_ackQueueLock.Lock())
            {
                while (_ackQueue.Last != null && _ackQueue.Last.Value.Sequence <= e.DeliveryTag)
                {
                    var msg = _ackQueue.Last.Value;
                    _ackQueue.RemoveLast();
                    msg.SetResult();
                }
            }
        }

        #endregion

        #region Topology handlers       

        private async Task TopologyConfigureAsync(ILinkTopologyConfig config)
        {
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(0)
                .ConfigureAwait(false);

            _exchage = await _topologyConfigHandler(config)
                .ConfigureAwait(false);
        }

        private async Task TopologyReadyAsync()
        {
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(0)
                .ConfigureAwait(false);

            lock (_sync)
            {
                _logger.Debug("Topology ready");
                _loopCancellationSource?.Cancel();
                _loopCancellationSource?.Dispose();
                // ReSharper disable once MethodSupportsCancellation                    
                _loopTask?.WaitWithoutException();                

                _loopCancellationSource = new CancellationTokenSource();
                _loopCancellation = _loopCancellationSource.Token;

                // ReSharper disable once MethodSupportsCancellation
                _loopTask = Task.Run(async () => await SendLoopAsync().ConfigureAwait(false));
            }
        }

        private async Task TopologyConfigurationErrorAsync(Exception ex)
        {
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(0)
                .ConfigureAwait(false);

            _logger.Warning($"Cannot configure topology for producer: {ex.Message}");
            await _topologyConfigErrorHandler(ex)
                .ConfigureAwait(false);
        }

        private void TopologyOnDisposed(object sender, EventArgs e)
        {
            _logger.Debug("Topology configurator disposed, disposing...");
            Dispose(true);
        }

        #endregion
    }
}