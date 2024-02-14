#region Usings

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Connection;
using RabbitLink.Exceptions;
using RabbitLink.Interceptors;
using RabbitLink.Internals;
using RabbitLink.Internals.Async;
using RabbitLink.Internals.Channels;
using RabbitLink.Internals.Lens;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Producer
{
    internal class LinkProducer : AsyncStateMachine<LinkProducerState>, ILinkProducerInternal, ILinkChannelHandler
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
            new CompositeChannel<LinkProducerMessage>(new LensChannel<LinkProducerMessage>());

        private readonly object _sync = new object();

        private readonly LinkTopologyRunner<ILinkExchange> _topologyRunner;

        private ILinkExchange _exchange;

        private volatile TaskCompletionSource<object> _readyCompletion =
            new TaskCompletionSource<object>();

        private readonly string _appId;
        private readonly PublishInvocation _decoratorsInvocation;
        private readonly HandlePublishDelegate _handlePublishDelegateCore;

        #endregion

        #region Ctor

        public LinkProducer(
            LinkProducerConfiguration configuration,
            ILinkChannel channel
        ) : base(LinkProducerState.Init)
        {
            _configuration = configuration;
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));

            _logger = _channel.Connection.Configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})")
                      ?? throw new InvalidOperationException("Cannot create logger");

            _topologyRunner =
                new LinkTopologyRunner<ILinkExchange>(_logger, _configuration.TopologyHandler.Configure);

            _appId = _channel.Connection.Configuration.AppId;

            _channel.Disposed += ChannelOnDisposed;

            _logger.Debug($"Created(channelId: {_channel.Id})");

            _channel.Initialize(this);

            var interceptors = configuration.PublishInterceptors;
            if (interceptors.Count > 0)
            {
                var invocation = new PublishInvocation();
                for (int i = interceptors.Count - 1; i >= 0; i--)
                {
                    invocation = new PublishInvocation(invocation, interceptors[i]);
                }

                _decoratorsInvocation = invocation;
            }

            _handlePublishDelegateCore = (publishMessage, ct) => PublishInternalAsync(publishMessage, ct);
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
                        try
                        {
                            await ActiveAsync(model, cancellation)
                                .ConfigureAwait(false);
                            newState = LinkProducerState.Stopping;
                        }
                        finally
                        {
                            if (_readyCompletion.Task.IsCompleted)
                                _readyCompletion = new TaskCompletionSource<object>();
                        }

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
                        throw new NotImplementedException($"Handler for state ${State} not implemented");
                }
            }
        }

        protected override void OnStateChange(LinkProducerState newState)
        {
            _logger.Debug($"State change {State} -> {newState}");

            try
            {
                _configuration.StateHandler(State, newState);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Exception in state handler: {ex}");
            }

            base.OnStateChange(newState);
        }

        public async Task OnConnecting(CancellationToken cancellation)
        {
            if (cancellation.IsCancellationRequested)
                return;

            try
            {
                await _messageQueue.YieldAsync(cancellation)
                                   .ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // No op
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
                try
                {
                    var correlationId = correlationValue switch
                    {
                        string value => value,
                        byte[] value => Encoding.UTF8.GetString(value),
                        _ => null
                    };

                    if (correlationId != null)
                    {
                        _ackQueue.Return(correlationId, info.ReplyText);
                    }
                }
                catch
                {
                    // no-op
                }
            }
        }

        #endregion

        #region ILinkProducerInternal Members

        public event EventHandler Disposed;
        public ILinkChannel Channel => _channel;

        public void Dispose()
        {
            Dispose(false);
        }

        public Task WaitReadyAsync(CancellationToken? cancellation = null)
        {
            return _readyCompletion.Task
                                   .ContinueWith(
                                       t => t.Result,
                                       cancellation ?? CancellationToken.None,
                                       TaskContinuationOptions.RunContinuationsAsynchronously,
                                       TaskScheduler.Current
                                   );
        }

        public Task PublishAsync<TBody>(ILinkPublishMessage<TBody> message, CancellationToken? cancellation = null)
            where TBody : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var props = message.Properties.Clone();
            ReadOnlyMemory<byte> body;

            try
            {
                if (_configuration.Serializer == null)
                    throw new InvalidOperationException("Serializer not set for producer");

                body = _configuration.Serializer.Serialize(message.Body, props);
            }
            catch (Exception ex)
            {
                throw new LinkSerializationException(ex);
            }

            var typeName = _configuration.TypeNameMapping.Map(typeof(TBody));
            if (typeName != null)
            {
                props.Type = typeName;
            }
            else if (!_configuration.TypeNameMapping.IsEmpty)
            {
                throw new LinkProducerTypeNameMappingException(typeof(TBody));
            }

            return PublishAsync(new LinkPublishMessage<ReadOnlyMemory<byte>>(body, props, message.PublishProperties), cancellation);
        }

        public Task PublishAsync(
            ILinkPublishMessage<ReadOnlyMemory<byte>> message,
            CancellationToken? cancellation = null
        )
        {
            if (State == LinkProducerState.Disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.PublishProperties.Mandatory == true && !ConfirmsMode)
                throw new LinkNotSupportedException("Mandatory without ConfirmsMode not supported");

            if (cancellation == null)
            {
                if (_configuration.PublishTimeout != TimeSpan.Zero &&
                    _configuration.PublishTimeout != Timeout.InfiniteTimeSpan)
                {
                    cancellation = new CancellationTokenSource(_configuration.PublishTimeout).Token;
                }
                else
                {
                    cancellation = CancellationToken.None;
                }
            }

            return _decoratorsInvocation != null
                ? _decoratorsInvocation.Execute(message, cancellation.Value, _handlePublishDelegateCore)
                : PublishInternalAsync(message, cancellation);
        }

        private Task PublishInternalAsync(
            ILinkPublishMessage<ReadOnlyMemory<byte>> message,
            CancellationToken? cancellation
        )
        {
            var msgProperties = _configuration.MessageProperties.Extend(message.Properties);
            msgProperties.AppId = _appId;

            var publishProperties = _configuration.PublishProperties.Extend(message.PublishProperties);

            var body = message.Body;

            _configuration.MessageIdGenerator.SetMessageId(
                body,
                msgProperties,
                publishProperties.Clone()
            );
            var msg = new LinkProducerMessage(body, msgProperties, publishProperties, cancellation.Value);

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
        public TimeSpan? PublishTimeout => _configuration.PublishTimeout;

        #endregion

        private void Stop()
        {
            var messages = _ackQueue.Reset();

            if (messages.Count > 0)
                _logger.Warning($"Requeuing {messages.Count} not ACKed or NACKed messages");


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

            _readyCompletion.TrySetResult(null);
            await AsyncHelper.RunAsync(() => ProcessQueue(model, cancellation))
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
                catch (Exception ex)
                {
                    if (cancellation.IsCancellationRequested)
                        continue;

                    _logger.Error($"Cannot read message from queue: {ex}");
                    return;
                }

                var seq = model.NextPublishSeqNo;
                var corellationId = _ackQueue.Add(message, seq);

                var properties = model.CreateBasicProperties();
                properties.Extend(message.Properties);

                if (_configuration.SetUserId)
                    properties.UserId = _channel.Connection.UserId;

                properties.Headers ??= new Dictionary<string, object>();
                properties.Headers[CorrelationHeader] = Encoding.UTF8.GetBytes(corellationId);

                try
                {
                    model.BasicPublish(
                        _exchange.Name,
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
                    _ackQueue.Ack(seq, false);
            }
        }

        private Task SetConfirmsModeAsync(IModel model)
        {
            if (ConfirmsMode)
                return AsyncHelper.RunAsync(model.ConfirmSelect);

            return Task.CompletedTask;
        }

        private async Task<bool> ConfigureAsync(IModel model, bool retry, CancellationToken cancellation)
        {
            if (retry)
            {
                try
                {
                    _logger.Debug($"Retrying in {_configuration.RecoveryInterval.TotalSeconds:0.###}s");
                    await Task.Delay(_configuration.RecoveryInterval, cancellation)
                              .ConfigureAwait(false);
                }
                catch
                {
                    return false;
                }
            }

            _logger.Debug("Configuring topology");

            try
            {
                _exchange = await _topologyRunner
                                  .RunAsync(model, cancellation)
                                  .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Exception on topology configuration: {ex}");

                try
                {
                    await _configuration.TopologyHandler.ConfigurationError(ex)
                                        .ConfigureAwait(false);
                }
                catch (Exception handlerException)
                {
                    _logger.Error($"Exception in topology error handler: {handlerException}");
                }

                return false;
            }

            _logger.Debug("Topology configured");

            return true;
        }

        private void ChannelOnDisposed(object sender, EventArgs eventArgs)
            => Dispose(true);

        private void Dispose(bool byChannel)
        {
            if (State == LinkProducerState.Disposed)
                return;

            lock (_sync)
            {
                if (State == LinkProducerState.Disposed)
                    return;

                _logger.Debug($"Disposing ( by channel: {byChannel} )");

                _channel.Disposed -= ChannelOnDisposed;
                if (!byChannel)
                    _channel.Dispose();

                var ex = new ObjectDisposedException(GetType().Name);
                _messageQueue.Dispose();

                ChangeState(LinkProducerState.Disposed);

                _readyCompletion.TrySetException(ex);

                _logger.Debug("Disposed");
                _logger.Dispose();

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Container for nesting interception calls into each other.
        /// </summary>
        private class PublishInvocation
        {
            private readonly PublishInvocation _next;
            private readonly IPublishInterceptor _interceptor;

            /// <summary>
            /// Creates container that will execute core logic on InterceptCall
            /// </summary>
            public PublishInvocation()
            {
            }

            /// <summary>
            /// Creates container that will execute passed interceptor (<see cref="interceptor"/>)
            /// and then delegate execution to next invocation (<see cref="next"/>).
            /// </summary>
            public PublishInvocation(PublishInvocation next, IPublishInterceptor interceptor)
            {
                _next = next;
                _interceptor = interceptor;
            }

            public Task Execute(ILinkPublishMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct, HandlePublishDelegate executeCore)
            {
                if (_next == null)
                    return executeCore(msg, ct);

                return _interceptor.Intercept(msg, ct, (message, cancellation) => _next.Execute(message, cancellation, executeCore));
            }
        }
    }
}
