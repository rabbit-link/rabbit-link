using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Connection;
using RabbitLink.Interceptors;
using RabbitLink.Internals;
using RabbitLink.Internals.Async;
using RabbitLink.Internals.Channels;
using RabbitLink.Internals.Lens;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitLink.Consumer
{
    internal class LinkConsumer : AsyncStateMachine<LinkConsumerState>, ILinkConsumerInternal, ILinkChannelHandler
    {
        private readonly LinkConsumerConfiguration _configuration;
        private readonly ILinkChannel _channel;
        private readonly ILinkLogger _logger;

        private readonly object _sync = new();

        private readonly ConsumerTagProviderDelegate _consumerTagProvider;
        private readonly LinkTopologyRunner<ILinkQueue> _topologyRunner;
        private ILinkQueue _queue;

        private volatile TaskCompletionSource<object> _readyCompletion = new();

        private readonly CompositeChannel<LinkConsumerMessageAction> _actionQueue =
            new(new LensChannel<LinkConsumerMessageAction>());

        private readonly DeliveryInvocation _decoratorsInvocation;
        private readonly HandleDeliveryDelegate _coreExecuteDelegate;

        private volatile EventingBasicConsumer _consumer;
        private volatile CancellationTokenSource _consumerCancellationTokenSource;

        private readonly string _appId;

        public LinkConsumer(
            LinkConsumerConfiguration configuration,
            ILinkChannel channel
        ) : base(LinkConsumerState.Init)
        {
            _configuration = configuration;

            _channel = channel ?? throw new ArgumentNullException(nameof(channel));

            _logger = _channel.Connection.Configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})")
                      ?? throw new InvalidOperationException("Cannot create logger");

            _topologyRunner = new LinkTopologyRunner<ILinkQueue>(_logger, configuration.TopologyHandler.Configure);
            _appId = _channel.Connection.Configuration.AppId;

            _consumerTagProvider = configuration.ConsumerTagProvider;

            _channel.Disposed += ChannelOnDisposed;

            _channel.Initialize(this);

            var interceptors = configuration.DeliveryInterceptors;
            if (interceptors.Count > 0)
            {
                var invocation = new DeliveryInvocation();
                for (int i = interceptors.Count - 1; i >= 0; i--)
                {
                    invocation = new DeliveryInvocation(invocation, interceptors[i]);
                }
                _decoratorsInvocation = invocation;
            }

            _coreExecuteDelegate = (message, _) => _configuration.MessageHandler(message);
        }

        public Guid Id { get; } = Guid.NewGuid();
        public ushort PrefetchCount => _configuration.PrefetchCount;
        public bool AutoAck => _configuration.AutoAck;
        public int Priority => _configuration.Priority;
        public bool CancelOnHaFailover => _configuration.CancelOnHaFailover;
        public bool Exclusive => _configuration.Exclusive;

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

        public event EventHandler Disposed;
        public ILinkChannel Channel => _channel;

        private void ChannelOnDisposed(object sender, EventArgs eventArgs)
            => Dispose(true);

        public void Dispose()
            => Dispose(false);

        private void Dispose(bool byChannel)
        {
            if (State == LinkConsumerState.Disposed)
                return;

            lock (_sync)
            {
                if (State == LinkConsumerState.Disposed)
                    return;

                _logger.Debug($"Disposing ( by channel: {byChannel} )");

                _channel.Disposed -= ChannelOnDisposed;
                if (!byChannel)
                {
                    _channel.Dispose();
                }

                var ex = new ObjectDisposedException(GetType().Name);

                ChangeState(LinkConsumerState.Disposed);

                _readyCompletion.TrySetException(ex);

                _logger.Debug("Disposed");
                _logger.Dispose();

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnStateChange(LinkConsumerState newState)
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

        public async Task OnActive(IModel model, CancellationToken cancellation)
        {
            var newState = LinkConsumerState.Init;

            while (true)
            {
                if (cancellation.IsCancellationRequested)
                {
                    newState = LinkConsumerState.Stopping;
                }

                ChangeState(newState);

                switch (State)
                {
                    case LinkConsumerState.Init:
                        newState = LinkConsumerState.Configuring;
                        break;
                    case LinkConsumerState.Configuring:
                    case LinkConsumerState.Reconfiguring:
                        newState = await ConfigureAsync(
                                model,
                                State == LinkConsumerState.Reconfiguring,
                                cancellation
                            )
                            .ConfigureAwait(false)
                            ? LinkConsumerState.Active
                            : LinkConsumerState.Reconfiguring;
                        break;
                    case LinkConsumerState.Active:
                        await ActiveAsync(model, cancellation)
                            .ConfigureAwait(false);

                        newState = LinkConsumerState.Stopping;
                        break;
                    case LinkConsumerState.Stopping:
                        await AsyncHelper.RunAsync(() => Stop(model))
                                         .ConfigureAwait(false);

                        if (cancellation.IsCancellationRequested)
                        {
                            ChangeState(LinkConsumerState.Init);
                            return;
                        }

                        newState = LinkConsumerState.Reconfiguring;
                        break;

                    default:
                        throw new NotImplementedException($"Handler for state ${State} not implemented");
                }
            }
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
                _queue = await _topologyRunner
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

        private async Task ActiveAsync(IModel model, CancellationToken cancellation)
        {
            try
            {
                await AsyncHelper.RunAsync(() => InitializeConsumer(model, cancellation))
                                 .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.Error($"Cannot initialize: {ex}");
                return;
            }

            using var ccs = CancellationTokenSource
                .CreateLinkedTokenSource(cancellation, _consumerCancellationTokenSource.Token);
            var token = ccs.Token;

            try
            {
                _readyCompletion.TrySetResult(null);

                await AsyncHelper.RunAsync(() => ProcessActionQueue(model, token))
                                 .ConfigureAwait(false);
            }
            catch
            {
                // no-op
            }
            finally
            {
                if (_readyCompletion.Task.IsCompleted)
                    _readyCompletion = new TaskCompletionSource<object>();
            }
        }

        private void ProcessActionQueue(IModel model, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                LinkConsumerMessageAction action;
                try
                {
                    action = _actionQueue.Wait(cancellation);
                }
                catch (Exception ex)
                {
                    if (cancellation.IsCancellationRequested)
                        continue;

                    _logger.Error($"Cannot read message from action queue: {ex}");
                    return;
                }

                try
                {
                    switch (action.Strategy)
                    {
                        case LinkConsumerAckStrategy.Ack:
                            model.BasicAck(action.Seq, false);
                            break;
                        case LinkConsumerAckStrategy.Nack:
                        case LinkConsumerAckStrategy.Requeue:
                            model.BasicNack(action.Seq, false, action.Strategy == LinkConsumerAckStrategy.Requeue);
                            break;
                        default:
                            throw new NotImplementedException($"AckStrategy {action.Strategy} not supported");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Cannot publish message: {ex.Message}");
                    return;
                }
            }
        }

        private void InitializeConsumer(IModel model, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            _consumerCancellationTokenSource = new CancellationTokenSource();

            _consumer = new EventingBasicConsumer(model);
            _consumer.Received += ConsumerOnReceived;
            _consumer.Registered += ConsumerOnRegistered;
            _consumer.ConsumerCancelled += ConsumerOnConsumerCancelled;

            cancellation.ThrowIfCancellationRequested();

            model.BasicQos(0, PrefetchCount, false);

            cancellation.ThrowIfCancellationRequested();

            var options = new Dictionary<string, object>();


            if (Priority != 0)
                options["x-priority"] = Priority;

            if (CancelOnHaFailover)
                options["x-cancel-on-ha-failover"] = CancelOnHaFailover;

            var consumerTag = _consumerTagProvider == null
                ? Id.ToString("D")
                : _consumerTagProvider(Id);
            model.BasicConsume(_queue.Name, AutoAck, consumerTag, false, Exclusive, options, _consumer);
        }

        private void ConsumerOnRegistered(object sender, ConsumerEventArgs e)
            => _logger.Debug($"Consuming: {string.Join(", ", e.ConsumerTags)}");


        private void ConsumerOnReceived(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var props = new LinkMessageProperties();
                props.Extend(e.BasicProperties);

                var receiveProps = new LinkReceiveProperties(e.Redelivered, e.Exchange, e.RoutingKey, _queue.Name,
                    props.AppId == _appId);

                var token = _consumerCancellationTokenSource.Token;

                var msg = new LinkConsumedMessage<ReadOnlyMemory<byte>>(e.Body, props, receiveProps, token);

                HandleMessageAsync(msg, e.DeliveryTag);
            }
            catch (Exception ex)
            {
                _logger.Error($"Receive message error, NACKing: {ex}");

                try
                {
                    _actionQueue.Put(new LinkConsumerMessageAction(
                        e.DeliveryTag,
                        LinkConsumerAckStrategy.Nack,
                        _consumerCancellationTokenSource.Token)
                    );
                }
                catch
                {
                    // No-op
                }
            }
        }

        private Task HandleMessageAsync(ILinkConsumedMessage<ReadOnlyMemory<byte>> msg, ulong deliveryTag)
        {
            var cancellation = msg.Cancellation;

            Task<LinkConsumerAckStrategy> task;

            try
            {
                task = _decoratorsInvocation != null
                    ? _decoratorsInvocation.Execute(msg, cancellation, _coreExecuteDelegate)
                    : _configuration.MessageHandler(msg);
            }
            catch (Exception ex)
            {
                task = Task.FromException<LinkConsumerAckStrategy>(ex);
            }

            return task.ContinueWith(
                t => OnMessageHandledAsync(t, deliveryTag, cancellation),
                cancellation,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Current
            );
        }

        private async Task OnMessageHandledAsync(
            Task<LinkConsumerAckStrategy> task,
            ulong deliveryTag,
            CancellationToken cancellation
        )
        {
            if (AutoAck) return;

            try
            {
                LinkConsumerMessageAction action;

                switch (task.Status)
                {
                    case TaskStatus.RanToCompletion:
                        action = new LinkConsumerMessageAction(deliveryTag, task.Result, cancellation);
                        break;
                    case TaskStatus.Faulted:
                        try
                        {
                            var taskEx = task.Exception.GetBaseException();
                            var strategy = _configuration.ErrorStrategy.HandleError(taskEx);
                            action = new LinkConsumerMessageAction(deliveryTag, strategy, cancellation);

                            _logger.Warning($"Error in MessageHandler (strategy: {action.Strategy}): {taskEx}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning($"Error in ErrorStrategy for Error, NACKing: {ex}");
                            action = new LinkConsumerMessageAction(deliveryTag, LinkConsumerAckStrategy.Nack,
                                cancellation);
                        }

                        break;
                    case TaskStatus.Canceled:
                        try
                        {
                            var strategy = _configuration.ErrorStrategy.HandleCancellation();
                            action = new LinkConsumerMessageAction(deliveryTag, strategy, cancellation);

                            _logger.Warning($"MessageHandler cancelled (strategy: {action.Strategy})");
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning($"Error in ErrorStrategy for Cancellation, NACKing: {ex}");
                            action = new LinkConsumerMessageAction(deliveryTag, LinkConsumerAckStrategy.Nack,
                                cancellation);
                        }

                        break;
                    default:
                        return;
                }

                await _actionQueue.PutAsync(action)
                                  .ConfigureAwait(false);
            }
            catch
            {
                //no-op
            }
        }

        private void ConsumerOnConsumerCancelled(object sender, ConsumerEventArgs e)
        {
            _logger.Debug($"Cancelled: {string.Join(", ", e.ConsumerTags)}");
            _consumerCancellationTokenSource?.Cancel();
            _consumerCancellationTokenSource?.Dispose();
        }

        private void Stop(IModel model)
        {
            if (_consumer != null)
            {
                try
                {
                    if (_consumer.IsRunning)
                    {
                        model.BasicCancel(_consumer.ConsumerTags.First());
                    }
                }
                catch
                {
                    //No-op
                }
                finally
                {
                    _consumer = null;
                }
            }
        }

        public async Task OnConnecting(CancellationToken cancellation)
        {
            if (cancellation.IsCancellationRequested)
                return;

            try
            {
                await _actionQueue.YieldAsync(cancellation)
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
            // no-op
        }

        public void MessageNack(BasicNackEventArgs info)
        {
            // no-op
        }

        public void MessageReturn(BasicReturnEventArgs info)
        {
            // no-op
        }

        /// <summary>
        /// Container for nesting interception calls into each other.
        /// </summary>
        private class DeliveryInvocation
        {
            private readonly DeliveryInvocation _next;
            private readonly IDeliveryInterceptor _interceptor;

            /// <summary>
            /// Creates container that will execute core logic on InterceptCall
            /// </summary>
            public DeliveryInvocation()
            {

            }

            /// <summary>
            /// Creates container that will execute passed interceptor (<see cref="interceptor"/>)
            /// and then delegate execution to next invocation (<see cref="next"/>).
            /// </summary>
            public DeliveryInvocation(DeliveryInvocation next, IDeliveryInterceptor interceptor)
            {
                _next = next;
                _interceptor = interceptor;
            }

            public Task<LinkConsumerAckStrategy> Execute(ILinkConsumedMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct, HandleDeliveryDelegate executeCore)
            {
                if (_interceptor == null)
                    return executeCore(msg, ct);

                return _interceptor.Intercept(msg, ct, (message, innerCt) => _next.Execute(message, innerCt, executeCore));
            }
        }
    }
}
