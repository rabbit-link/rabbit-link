#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx.Synchronous;
using RabbitLink.Configuration;
using RabbitLink.Connection;
using RabbitLink.Exceptions;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Serialization;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkPullConsumer : ILinkPullConsumerInternal
    {
        #region .ctor

        public LinkPullConsumer(
            LinkConsumerConfiguration configuration,
            LinkConfiguration linkConfiguration,
            ILinkChannel channel,
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfigHandler,
            Func<Exception, Task> topologyConfigErrorHandler
            )
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
            _logger = linkConfiguration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(linkConfiguration.LoggerFactory));

            _topologyConfigHandler = topologyConfigHandler;
            _topologyConfigErrorHandler = topologyConfigErrorHandler;

            _channel = channel;
            _channel.Shutdown += ChannelOnShutdown;
            _topology = new LinkTopology(linkConfiguration, _channel,
                new LinkActionsTopologyHandler(TopologyConfigure, TopologyReady, TopologyConfigurationError), false);
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
            if (_disposedCancellation.IsCancellationRequested)
                return;

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _logger.Debug("Disposing");

                _disposedCancellation.Cancel();

                _topology.Disposed -= TopologyOnDisposed;
                _topology.Dispose();
                _channel.Dispose();

                _initializeCancellation?.Cancel();
                _initializeTask.WaitWithoutException();

                _logger.Debug("Disposed");
                _logger.Dispose();
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        internal async Task<LinkConsumerMessageQueue.QueueMessage> PrivateGetRawMessageAsync(
            CancellationToken? cancellation = null)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (cancellation == null)
            {
                cancellation = CancellationToken.None;
            }

            try
            {
                return await _messageQueue.GetMessage(cancellation.Value)
                    .ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        #region Work

        private async Task InitializeConsumer(CancellationToken cancellation)
        {
            try
            {
                // Initialize consumer
                await _channel.InvokeActionAsync(model =>
                {
                    if (cancellation.IsCancellationRequested)
                        return;

                    _consumer = new EventingBasicConsumer(model);
                    _consumer.Received += ConsumerOnReceived;
                    _consumer.ConsumerCancelled += ConsumerOnConsumerCancelled;
                    _consumer.Registered += ConsumerOnRegistered;

                    if (cancellation.IsCancellationRequested)
                        return;

                    model.BasicQos(0, PrefetchCount, false);

                    if (cancellation.IsCancellationRequested)
                        return;

                    var options = new Dictionary<string, object>
                    {
                        {"x-priority", Priority},
                        {"x-cancel-on-ha-failover", CancelOnHaFailover}
                    };

                    model.BasicConsume(_queue.Name, AutoAck, Id.ToString("D"), false, Exclusive, options, _consumer);
                }, cancellation);
            }
            catch (ObjectDisposedException)
            {
                _logger.Warning("Channel disposed, disposing");
#pragma warning disable 4014
                Task.Run(() => Dispose());
#pragma warning restore 4014                
            }
            catch (Exception ex)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _logger.Error("Cannot initialize: {0}", ex);

                if (_channel.IsOpen)
                {
                    _topology.ScheduleConfiguration(true);
                }
            }
        }

        #endregion

        #region Channel handlers

        private void ChannelOnShutdown(object sender, ShutdownEventArgs shutdownEventArgs)
        {
            _logger.Info("Channel shutdown, cancelling messages");
            _messageQueue.CancelMessages();
            _logger.Debug("All messages cancelled");
        }

        #endregion

        #region Public methods        

        public async Task<ILinkAckableRecievedMessage<object>> GetMessageAsync(CancellationToken? cancellation = null)
        {
            var rawMessage = await PrivateGetRawMessageAsync(cancellation)
                .ConfigureAwait(false);

            var typeName = rawMessage.Message.Properties.Type?.Trim();

            if (!string.IsNullOrEmpty(typeName))
            {
                var type = _configuration.TypeNameMapping.Map(typeName);
                if (type != null)
                {
                    ILinkMessage<object> message;
                    try
                    {
                        message = _configuration.MessageSerializer.Deserialize(type, rawMessage.Message);
                    }
                    catch (Exception ex)
                    {
                        rawMessage.Nack?.Invoke(false);
                        throw new LinkDeserializationException(rawMessage.Message, ex);
                    }

                    return LinkGenericMessageFactory.ConstructLinkAckableRecievedMessage(
                        type,
                        message,
                        rawMessage.Message.RecievedProperties,
                        rawMessage.Ack,
                        rawMessage.Nack
                        );
                }
            }

            return new LinkAckableRecievedMessage<byte[]>(rawMessage.Message, rawMessage.Ack, rawMessage.Nack);
        }

        public async Task<ILinkAckableRecievedMessage<T>> GetMessageAsync<T>(CancellationToken? cancellation = null)
            where T : class
        {
            var rawMessage = await PrivateGetRawMessageAsync(cancellation)
                .ConfigureAwait(false);

            if (typeof (T) == typeof (byte[]))
            {
                return new LinkAckableRecievedMessage<T>((ILinkRecievedMessage<T>) rawMessage.Message, rawMessage.Ack,
                    rawMessage.Nack);
            }

            ILinkMessage<T> message;

            try
            {
                message = _configuration.MessageSerializer.Deserialize<T>(rawMessage.Message);
            }
            catch (Exception ex)
            {
                rawMessage.Nack?.Invoke(false);
                throw new LinkDeserializationException(rawMessage.Message, ex);
            }

            return new LinkAckableRecievedMessage<T>(
                message,
                rawMessage.Message.RecievedProperties,
                rawMessage.Ack,
                rawMessage.Nack
                );
        }

        #endregion

        #region .fields

        private readonly LinkConsumerConfiguration _configuration;
        private readonly ILinkLogger _logger;
        private readonly Func<ILinkTopologyConfig, Task<ILinkQueue>> _topologyConfigHandler;
        private readonly Func<Exception, Task> _topologyConfigErrorHandler;
        private readonly ILinkChannel _channel;
        private readonly ILinkTopology _topology;
        private readonly LinkConsumerMessageQueue _messageQueue = new LinkConsumerMessageQueue();

        private ILinkQueue _queue;
        private Task _initializeTask;
        private readonly object _sync = new object();

        private readonly CancellationTokenSource _disposedCancellation =
            new CancellationTokenSource();

        private CancellationTokenSource _initializeCancellation;
        private EventingBasicConsumer _consumer;

        #endregion

        #region Properties

        public Guid Id { get; } = Guid.NewGuid();
        public ushort PrefetchCount => _configuration.PrefetchCount;
        public bool AutoAck => _configuration.AutoAck;
        public TimeSpan? GetMessageTimeout => _configuration.GetMessageTimeout;
        public int Priority => _configuration.Priority;
        public bool CancelOnHaFailover => _configuration.CancelOnHaFailover;
        public bool Exclusive => _configuration.Exclusive;

        #endregion

        #region Consumer handlers

        private void ConsumerOnRegistered(object sender, ConsumerEventArgs e)
        {
            _logger.Info("Consuming: {0}", e.ConsumerTag);
        }

        private void ConsumerOnConsumerCancelled(object sender, ConsumerEventArgs e)
        {
            _logger.Info("Cancelled: {0}", e.ConsumerTag);
            if (_channel.IsOpen)
            {
                _topology.ScheduleConfiguration(true);
            }
        }

        private void ConsumerOnReceived(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                _logger.Debug(
                    $"Message recieved, deliveryTag: {e.DeliveryTag}, exchange: {e.Exchange}, routingKey: {e.RoutingKey}, redelivered: {e.Redelivered}");

                Action<CancellationToken> ackAction;
                Action<CancellationToken, bool> nackAction;


                if (AutoAck)
                {
                    ackAction = cancellation => { };
                    nackAction = (cancellation, requeue) => { };
                }
                else
                {
                    ackAction = cancellation =>
                    {
                        if (cancellation.IsCancellationRequested)
                            return;

                        try
                        {
                            _channel.InvokeActionAsync(model => model.BasicAck(e.DeliveryTag, false), cancellation);
                        }
                        catch
                        {
                            // no op
                        }
                    };
                    nackAction = (cancellation, requeue) =>
                    {
                        if (cancellation.IsCancellationRequested)
                            return;

                        try
                        {
                            _channel.InvokeActionAsync(model => model.BasicNack(e.DeliveryTag, false, requeue),
                                cancellation);
                        }
                        catch
                        {
                            //no op
                        }
                    };
                }

                var msg = new LinkRecievedMessage<byte[]>(
                    e.Body,
                    new LinkMessageProperties(e.BasicProperties),
                    new LinkRecievedMessageProperties(e.Redelivered, e.Exchange, e.RoutingKey, _queue.Name)
                    );

                _messageQueue.Enqueue(msg, ackAction, nackAction);
            }
            catch (Exception ex)
            {
                _logger.Warning("Cannot add recieved message to queue: {0}", ex);
            }

            _logger.Info("Ready");
        }

        #endregion

        #region Topology handlers       

        private async Task TopologyConfigure(ILinkTopologyConfig config)
        {
            _queue = await Task.Run(async () => await _topologyConfigHandler(config)
                .ConfigureAwait(false), _disposedCancellation.Token)
                .ConfigureAwait(false);
        }

        private Task TopologyReady()
        {
            return Task.Run(() =>
            {
                lock (_sync)
                {
                    _logger.Debug("Topology ready");
                    _initializeCancellation?.Cancel();
                    _initializeTask?.WaitWithoutException();

                    _initializeCancellation = new CancellationTokenSource();
                    _initializeTask = Task.Run(
                        async () => await InitializeConsumer(_initializeCancellation.Token).ConfigureAwait(false),
                        _initializeCancellation.Token
                        );
                }
            });
        }

        private Task TopologyConfigurationError(Exception ex)
        {
            return Task.Run(async () =>
            {
                _logger.Warning("Cannot configure topology: {0}", ex.Message);
                await _topologyConfigErrorHandler(ex)
                    .ConfigureAwait(false);
            });
        }

        private void TopologyOnDisposed(object sender, EventArgs e)
        {
            _logger.Debug("Topology configurator disposed, disposing...");
            Dispose();
        }

        #endregion
    }
}