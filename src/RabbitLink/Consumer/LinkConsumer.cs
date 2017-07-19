#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Configuration;
using RabbitLink.Connection;
using RabbitLink.Exceptions;
using RabbitLink.Internals.Async;
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
    internal class LinkConsumer : ILinkConsumerInternal
    {
        #region Fields

        private readonly ILinkChannel _channel;

        private readonly LinkConsumerConfiguration _configuration;
        private readonly CancellationToken _disposedCancellation;

        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly LinkConfiguration _linkConfiguration;
        private readonly ILinkLogger _logger;
        private readonly LinkConsumerMessageQueue _messageQueue = new LinkConsumerMessageQueue();
        private readonly object _sync = new object();
        private readonly Func<Exception, Task> _topologyConfigErrorHandler;
        private readonly Func<ILinkTopologyConfig, Task<ILinkQueue>> _topologyConfigHandler;
        private EventingBasicConsumer _consumer;
        private CancellationToken _initializeCancellation;

        private CancellationTokenSource _initializeCancellationSource;
        private Task _initializeTask;

        private ILinkQueue _queue;

        #endregion

        #region Ctor

        public LinkConsumer(
            LinkConsumerConfiguration configuration,
            LinkConfiguration linkConfiguration,
            ILinkChannel channel,
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfigHandler,
            Func<Exception, Task> topologyConfigErrorHandler
        )
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _linkConfiguration = linkConfiguration ?? throw new ArgumentNullException(nameof(linkConfiguration));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _topologyConfigHandler = topologyConfigHandler ??
                                     throw new ArgumentNullException(nameof(topologyConfigHandler));
            _topologyConfigErrorHandler = topologyConfigErrorHandler ??
                                          throw new ArgumentNullException(nameof(topologyConfigErrorHandler));

            _logger = linkConfiguration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(linkConfiguration.LoggerFactory));


            _disposedCancellationSource = new CancellationTokenSource();
            _disposedCancellation = _disposedCancellationSource.Token;

            _logger.Debug($"Created(channelId: {_channel.Id})");
            
        }

        #endregion

        #region ILinkConsumerInternal Members

        public event EventHandler Disposed;

        public void Dispose()
        {
            Dispose(false);
        }

        public async Task<ILinkMessage<object>> GetMessageAsync(CancellationToken? cancellation = null)
        {
            var rawMessage = await PrivateGetRawMessageAsync(cancellation)
                .ConfigureAwait(false);

            var typeName = rawMessage.Properties.Type?.Trim();

            if (!string.IsNullOrEmpty(typeName))
            {
                var type = _configuration.TypeNameMapping.Map(typeName);
                if (type != null)
                {
                    object body;
                    try
                    {
                        body = _configuration.MessageSerializer.Deserialize(type, rawMessage.Body,
                            rawMessage.Properties);
                    }
                    catch (Exception ex)
                    {
#pragma warning disable 4014
                        rawMessage.NackAsync(CancellationToken.None);
#pragma warning restore 4014
                        throw new LinkDeserializationException(rawMessage.Body, rawMessage.Properties,
                            rawMessage.RecieveProperties, ex);
                    }

                    return LinkMessage<object>.Create(type, body, rawMessage);
                }
            }

            return rawMessage;
        }

        public async Task<ILinkMessage<T>> GetMessageAsync<T>(CancellationToken? cancellation = null)
            where T : class
        {
            var rawMessage = await PrivateGetRawMessageAsync(cancellation)
                .ConfigureAwait(false);

            if (typeof(T) == typeof(byte[]))
            {
                return rawMessage as ILinkMessage<T>;
            }

            T body;
            try
            {
                body = _configuration.MessageSerializer.Deserialize<T>(rawMessage.Body, rawMessage.Properties);
            }
            catch (Exception ex)
            {
#pragma warning disable 4014
                rawMessage.NackAsync(CancellationToken.None);
#pragma warning restore 4014
                throw new LinkDeserializationException(rawMessage.Body, rawMessage.Properties,
                    rawMessage.RecieveProperties, ex);
            }

            return new LinkMessage<T>(
                body,
                rawMessage
            );
        }

        public Guid Id { get; } = Guid.NewGuid();
        public ushort PrefetchCount => _configuration.PrefetchCount;
        public bool AutoAck => _configuration.AutoAck;
        public TimeSpan? GetMessageTimeout => _configuration.GetMessageTimeout;
        public int Priority => _configuration.Priority;
        public bool CancelOnHaFailover => _configuration.CancelOnHaFailover;
        public bool Exclusive => _configuration.Exclusive;

        #endregion

        private void Dispose(bool byTopology)
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _logger.Debug("Disposing");

                _disposedCancellationSource.Cancel();
                _disposedCancellationSource.Dispose();

                _topology.Disposed -= TopologyOnDisposed;
                if (!byTopology)
                {
                    _topology.Dispose();
                }

                _initializeCancellationSource?.Cancel();
                _initializeCancellationSource?.Dispose();

                // ReSharper disable once MethodSupportsCancellation
                _initializeTask.WaitWithoutException();

                _messageQueue.Dispose();

                _logger.Debug("Disposed");
                _logger.Dispose();
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        private async Task<LinkMessage<byte[]>> PrivateGetRawMessageAsync(
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
                return await _messageQueue.GetMessageAsync(cancellation.Value)
                    .ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

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
                // ReSharper disable MethodSupportsCancellation                
                Task.Run(() => Dispose());
                // ReSharper restore MethodSupportsCancellation
#pragma warning restore 4014                
            }
            catch (Exception ex)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _logger.Error($"Cannot initialize: {ex}");

                if (_channel.IsOpen)
                {
                    _topology.ScheduleConfiguration(true);
                }
            }
        }

        private void ChannelOnShutdown(object sender, ShutdownEventArgs shutdownEventArgs)
        {
            _logger.Info("Channel shutdown, cancelling messages");
            _messageQueue.CancelMessages();
            _logger.Debug("All messages cancelled");
        }

        private void ConsumerOnRegistered(object sender, ConsumerEventArgs e)
        {
            _logger.Info($"Consuming: {e.ConsumerTag}");
        }

        private void ConsumerOnConsumerCancelled(object sender, ConsumerEventArgs e)
        {
            _logger.Info("Cancelled: {e.ConsumerTag}");
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

                LinkMessageOnAckAsyncDelegate ackHandler = null;
                LinkMessageOnNackAsyncDelegate nackHandler = null;

                if (!AutoAck)
                {
                    ackHandler = async (onSuccess, cancellation) =>
                    {
                        cancellation.ThrowIfCancellationRequested();

                        try
                        {
                            await
                                _channel.InvokeActionAsync(model =>
                                        {
                                            _logger.Debug(
                                                $"Sending ACK for message with delivery tag: {e.DeliveryTag}");
                                            model.BasicAck(e.DeliveryTag, false);
                                            onSuccess?.Invoke();
                                        },
                                        cancellation)
                                    .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            throw new LinkMessageOperationException(
                                "Cannot complete message operation, see inner exception", ex);
                        }
                    };

                    nackHandler = async (requeue, onSuccess, cancellation) =>
                    {
                        cancellation.ThrowIfCancellationRequested();

                        try
                        {
                            await _channel.InvokeActionAsync(model =>
                                    {
                                        model.BasicNack(e.DeliveryTag, false, requeue);
                                        onSuccess?.Invoke();
                                    },
                                    cancellation)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            throw new LinkMessageOperationException(
                                "Cannot complete message operation, see inner exception", ex);
                        }
                    };
                }

                var properties = new LinkMessageProperties(e.BasicProperties);
                var recieveProperties = new LinkRecieveMessageProperties(
                    e.Redelivered,
                    e.Exchange,
                    e.RoutingKey,
                    _queue.Name,
                    _linkConfiguration.AppId != null &&
                    properties.AppId != null &&
                    _linkConfiguration.AppId == properties.AppId
                );

                _messageQueue.Enqueue(e.Body, properties, recieveProperties, ackHandler, nackHandler);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Cannot add recieved message to queue: {ex}");
            }
        }

        private async Task TopologyConfigure(ILinkTopologyConfig config)
        {
            _queue = await Task.Run(async () => await _topologyConfigHandler(config)
                    .ConfigureAwait(false), _disposedCancellation)
                .ConfigureAwait(false);
        }

        private Task TopologyReadyAsync()
        {
            // ReSharper disable MethodSupportsCancellation
            return Task.Run(() =>
            {
                lock (_sync)
                {
                    _logger.Debug("Topology ready");
                    _initializeCancellationSource?.Cancel();
                    _initializeCancellationSource?.Dispose();
                    _initializeTask?.WaitWithoutException();

                    _initializeCancellationSource = new CancellationTokenSource();
                    _initializeCancellation = _initializeCancellationSource.Token;

                    _initializeTask = Task.Run(
                        async () => await InitializeConsumer(_initializeCancellation).ConfigureAwait(false),
                        _initializeCancellation
                    );
                }
            });
            // ReSharper restore MethodSupportsCancellation
        }

        private Task TopologyConfigurationErrorAsync(Exception ex)
        {
            // ReSharper disable MethodSupportsCancellation
            return Task.Run(async () =>
            {
                _logger.Warning($"Cannot configure topology: {ex.Message}");
                await _topologyConfigErrorHandler(ex)
                    .ConfigureAwait(false);
            });
            // ReSharper restore MethodSupportsCancellation
        }

        private void TopologyOnDisposed(object sender, EventArgs e)
        {
            _logger.Debug("Topology configurator disposed, disposing...");
            Dispose(true);
        }
    }
}