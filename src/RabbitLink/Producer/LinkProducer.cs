#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Connection;
using RabbitLink.Internals;
using RabbitLink.Internals.Async;
using RabbitLink.Internals.Channels;
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
    internal class LinkProducer : AsyncStateMachine<LinkProducerState>, ILinkProducerIntenal, ILinkChannelHandler
    {
        #region Static fields

        private const string CorrelationHeader = "X-Producer-Corellation-Id";

        #endregion

        #region Fields

        private readonly LinkProducerAckQueue _ackQueue =
            new LinkProducerAckQueue();

        private readonly ILinkChannel _channel;
        private readonly LinkProducerConfiguration _configuration;

        private readonly ILinkLogger _logger;

        private readonly CompositeChannel<LinkProducerMessage> _messageQueue =
            new CompositeChannel<LinkProducerMessage>();

        private readonly object _sync = new object();

        private readonly ILinkProducerTopologyHandler _topologyHandler;
        private readonly LinkTopologyRunner<ILinkExchage> _topologyRunner;

        private ILinkExchage _exchage;

        #endregion

        #region Ctor

        public LinkProducer(
            LinkProducerConfiguration configuration, 
            LinkConfiguration linkConfiguration,
            ILinkChannel channel,
            ILinkProducerTopologyHandler topologyHandler
        ) : base(LinkProducerState.Init)
        {
            _configuration = configuration;

            _topologyHandler = topologyHandler ?? throw new ArgumentNullException(nameof(topologyHandler));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));

            _logger = linkConfiguration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");
            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(linkConfiguration.LoggerFactory));

            _topologyRunner =
                new LinkTopologyRunner<ILinkExchage>(_logger, _topologyHandler.Configure);
            _channel.Disposed += ChannelOnDisposed;

            _logger.Debug($"Created(channelId: {_channel.Id})");

            _channel.Initialize(this);
        }

        #endregion

        #region ILinkChannelHandler Members

        public async Task OnActive(IModel model, CancellationToken cancellation)
        {
            var newState = LinkProducerState.Init;

            while (true)
            {
                if (cancellation.IsCancellationRequested)
                {
                    newState = LinkProducerState.Stopping;
                }

                ChangeState(newState);

                switch (State)
                {
                    case LinkProducerState.Init:
                        newState = LinkProducerState.Configuring;
                        break;
                    case LinkProducerState.Configuring:
                    case LinkProducerState.Reconfiguring:
                        newState = await ConfigureAsync(
                                model,
                                State == LinkProducerState.Reconfiguring,
                                cancellation
                            )
                            .ConfigureAwait(false)
                            ? LinkProducerState.Active
                            : LinkProducerState.Reconfiguring;
                        break;
                    case LinkProducerState.Active:
                        await ActiveAsync(model, cancellation)
                            .ConfigureAwait(false);
                        newState = LinkProducerState.Reconfiguring;
                        break;
                    case LinkProducerState.Stopping:
                        await AsyncHelper.RunAsync(Stop)
                            .ConfigureAwait(false);
                        if (cancellation.IsCancellationRequested)
                        {
                            ChangeState(LinkProducerState.Init);
                            return;
                        }
                        newState = LinkProducerState.Reconfiguring;
                        break;
                    default:
                        throw new NotImplementedException($"Handler for state ${State} not implemeted");
                }
            }
        }

        protected override void OnStateChange(LinkProducerState newState)
        {
            _logger.Debug($"State change {State} -> {newState}");
            base.OnStateChange(newState);
        }

        public async Task OnConnecting(CancellationToken cancellation)
        {
            try
            {
                await _messageQueue.YieldAsync(cancellation)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // No op
            }
        }

        public void MessageAck(BasicAckEventArgs info)
        {
            _ackQueue.Ack(info.DeliveryTag, info.Multiple);
        }

        public void MessageNack(BasicNackEventArgs info)
        {
            _ackQueue.Nack(info.DeliveryTag, info.Multiple);
        }

        public void MessageReturn(BasicReturnEventArgs info)
        {
            if (info.BasicProperties.Headers.TryGetValue(CorrelationHeader, out var correlationValue))
            {
                var correlationId = correlationValue as string;
                if (correlationId != null)
                {
                    _ackQueue.Return(correlationId, info.ReplyText);
                }
            }
        }

        #endregion

        #region ILinkProducerIntenal Members

        public event EventHandler Disposed;

        public void Dispose()
        {
            Dispose(false);
        }

        public Task PublishAsync(
            LinkPublishMessage message,
            CancellationToken? cancellation = null
        )
        {
            if (State == LinkProducerState.Disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (cancellation == null)
            {
                cancellation = _configuration.PublishTimeout != null
                    ? new CancellationTokenSource(_configuration.PublishTimeout.Value).Token
                    : CancellationToken.None;
            }

            var msgProperties = message.Properties.Clone();
            msgProperties.AppId = _linkConfiguration.AppId;

            if (_configuration.SetUserId)
                msgProperties.UserId = _channel.Connection.UserId;

            _configuration.MessageIdGenerator.SetMessageId(
                message.Body, 
                msgProperties.Clone(), 
                message.PublishProperties.Clone()
            );
            
            var msg = new LinkProducerMessage(message.Body, msgProperties, message.PublishProperties, cancellation.Value);

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

        public Guid Id { get; } = Guid.NewGuid();

        public bool ConfirmsMode => _configuration.ConfirmsMode;

        public LinkPublishProperties PublishProperties => _configuration.PublishProperties.Clone();
        public LinkMessageProperties MessageProperties => _configuration.MessageProperties.Clone();
        public TimeSpan? PublishTimeout => _configuration.PublishTimeout;

        #endregion

        private void Stop()
        {
            var messages = _ackQueue.Reset();

            if (messages.Count > 0)
            {
                _logger.Warning($"Requeuing {messages.Count} not ACKed or NACKed messages");
            }

            _messageQueue.PutRetry(messages, CancellationToken.None);
        }

        private async Task ActiveAsync(IModel model, CancellationToken cancellation)
        {
            try
            {
                await SetConfirmsModeAsync(model)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Confirms mode activation error: {ex}");
                return;
            }

            await AsyncHelper.RunAsync(()=>ProcessQueue(model, cancellation))
                .ConfigureAwait(false);
        }

        private void ProcessQueue(IModel model, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                LinkProducerMessage message;
                try
                {
                    message = _messageQueue.Wait(cancellation);
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Cannot read message from queue: {ex}");
                    return;
                }

                var seq = model.NextPublishSeqNo;
                var corellationId = _ackQueue.Add(message, seq);

                var properties = model.CreateBasicProperties();
                properties.Extend(message.Properties);

                if (properties.Headers == null)
                    properties.Headers = new Dictionary<string, object>();

                properties.Headers[CorrelationHeader] = corellationId;

                try
                {
                    model.BasicPublish(
                        _exchage.Name,
                        message.PublishProperties.RoutingKey ?? "",
                        message.PublishProperties.Mandatory ?? false,
                        properties,
                        message.Body
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error($"Cannot publish message: {ex.Message}");
                    return;
                }

                if (!ConfirmsMode)
                {
                    _ackQueue.Ack(seq, false);
                }
            }
        }

        private Task SetConfirmsModeAsync(IModel model)
        {
            if (ConfirmsMode)
            {
                return AsyncHelper.RunAsync(model.ConfirmSelect);
            }

            return Task.CompletedTask;
        }

        private async Task<bool> ConfigureAsync(IModel model, bool retry, CancellationToken cancellation)
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
                    return false;
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
                    await _topologyHandler.ConfigurationError(ex)
                        .ConfigureAwait(false);
                }
                catch (Exception handlerException)
                {
                    _logger.Error($"Exception in topology error handler: {handlerException}");
                }

                return false;
            }

            _logger.Info("Topology configured");

            return true;
        }

        private void ChannelOnDisposed(object sender, EventArgs eventArgs)
        {
            _logger.Debug("Channel disposed, disposing...");
            Dispose(true);
        }

        private void Dispose(bool byChannel)
        {
            if (State == LinkProducerState.Disposed)
                return;

            lock (_sync)
            {
                if (State == LinkProducerState.Disposed)
                    return;

                _logger.Debug("Disposing");

                _channel.Disposed -= ChannelOnDisposed;
                if (!byChannel)
                {
                    _channel.Dispose();
                }

                var ex = new ObjectDisposedException(GetType().Name);
                _messageQueue.Complete(msg => msg.TrySetException(ex));

                ChangeState(LinkProducerState.Disposed);
                _logger.Debug("Disposed");
                _logger.Dispose();

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}