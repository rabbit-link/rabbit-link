#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
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
            if (_disposedCancellation.IsCancellationRequested) return;

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested) return;

                _logger.Debug("Disposing");

                _disposedCancellationSource.Cancel();
                _disposedCancellationSource.Dispose();

                _topology.Disposed -= TopologyOnDisposed;
                _topology.Dispose();
                _channel.Dispose();

                _loopCancellationSource?.Cancel();
                _loopCancellationSource?.Dispose();

                _publishQueue.CompleteAdding();
                // ReSharper disable once MethodSupportsCancellation
                _loopTask?.WaitAndUnwrapException();
                _loopTask?.Dispose();                

                // cancelling requests
                Parallel.ForEach(_ackQueue,
                    msg => { msg.SetException(new ObjectDisposedException(GetType().Name)); });

                Parallel.ForEach(_retryQueue,
                    msg => { msg.SetException(new ObjectDisposedException(GetType().Name)); });

                Parallel.ForEach(_publishQueue.GetConsumingEnumerable(),
                    msg => { msg.SetException(new ObjectDisposedException(GetType().Name)); });

                _ackQueue.Clear();
                _retryQueue.Clear();
                _publishQueue.Dispose();

                _logger.Debug("Disposed");
                _logger.Dispose();
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public methods        

        public Task PublishAsync<T>(ILinkMessage<T> message, LinkPublishProperties properties = null,
            CancellationToken? cancellation = null) where T : class
        {
            var msg = new LinkMessage<T>(message.Body, message.Properties);

            if (typeof (T) == typeof (byte[]))
            {
                return PublishRawAsync((ILinkMessage<byte[]>) msg, properties, cancellation);
            }

            ILinkMessage<byte[]> rawMsg;

            try
            {
                rawMsg = _configuration.MessageSerializer.Serialize(msg);
            }
            catch (Exception ex)
            {
                throw new LinkSerializationException(ex);
            }

            var typeName = _configuration.TypeNameMapping.Map<T>();
            if (typeName != null)
            {
                rawMsg.Properties.Type = typeName;
            }

            return PublishRawAsync(rawMsg, properties, cancellation);
        }

        #endregion

        #region Private methods

        private async Task PublishRawAsync(ILinkMessage<byte[]> message, LinkPublishProperties properties = null,
            CancellationToken? cancellation = null)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Properties == null)
                throw new ArgumentNullException(nameof(message.Properties));

            if (cancellation == null)
            {
                cancellation = _configuration.PublishTimeout != null
                    ? new CancellationTokenSource(_configuration.PublishTimeout.Value).Token
                    : CancellationToken.None;
            }            

            var publishProperties = _configuration.PublishProperties.Clone();
            publishProperties.Extend(properties ?? new LinkPublishProperties());

            var messageProperties = _configuration.MessageProperties.Clone();
            messageProperties.CopyFrom(message.Properties);
            messageProperties.AppId = _linkConfiguration.AppId;         

            if (_configuration.SetUserId)
            {
                messageProperties.UserId = _channel.Connection.UserId;
            }

            message = new LinkMessage<byte[]>(message.Body, messageProperties);
            _configuration.MessageIdStrategy.SetMessageId(message);            

            var holder = new MessageHolder(message.Body, message.Properties, publishProperties, cancellation.Value);

            try
            {
                await _publishQueue.EnqueueAsync(holder, cancellation.Value)
                    .ConfigureAwait(false);
            }
            catch
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            await holder.Task
                .ConfigureAwait(false);
        }

        #endregion

        #region Private classes

        private class MessageHolder
        {
            private readonly IDisposable _cancellationRegistration;
            private readonly TaskCompletionSource _completion = new TaskCompletionSource();

            public MessageHolder(byte[] body, LinkMessageProperties properties, LinkPublishProperties publishProperties,
                CancellationToken cancellation)
            {
                Cancellation = cancellation;
                Body = body;
                Properties = properties;
                PublishProperties = publishProperties;

                _cancellationRegistration = Cancellation.Register(() =>
                {
                    if (!Processing)
                    {
                        SetCanceled();
                    }
                });
            }

            public void SetResult()
            {
                _completion.TrySetResult();
                _cancellationRegistration.Dispose();
            }

            public void SetCanceled()
            {
                _completion.TrySetCanceled();
                _cancellationRegistration.Dispose();
            }

            public void SetException(Exception exception)
            {
                _completion.TrySetException(exception);
                _cancellationRegistration.Dispose();
            }

            public byte[] Body { get; }
            public LinkMessageProperties Properties { get; }
            public LinkPublishProperties PublishProperties { get; }
            public ulong Sequence { get; set; }
            public Task Task => _completion.Task;
            public CancellationToken Cancellation { get; }
            public bool Processing { get; set; }
        }

        #endregion

        #region Fields

        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly CancellationToken _disposedCancellation;

        private readonly AsyncProducerConsumerQueue<MessageHolder> _publishQueue =
            new AsyncProducerConsumerQueue<MessageHolder>();

        private readonly Queue<MessageHolder> _ackQueue = new Queue<MessageHolder>();
        private readonly Queue<MessageHolder> _retryQueue = new Queue<MessageHolder>();
        private readonly Func<ILinkTopologyConfig, Task<ILinkExchage>> _topologyConfigHandler;
        private readonly Func<Exception, Task> _topologyConfigErrorHandler;

        private Task _loopTask;
        private CancellationTokenSource _loopCancellationSource;
        private CancellationToken _loopCancellation;

        private readonly object _syncQueue = new object();
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

        private void RequeueUnacked()
        {
            lock (_syncQueue)
            {
                var count = _retryQueue.Count;

                if (!_ackQueue.Any())
                    return;

                _logger.Warning($"Requeuing {_ackQueue.Count} not ACKed or NACKed messages");

                while (_ackQueue.Any())
                {
                    var msg = _ackQueue.Dequeue();
                    if (msg.Cancellation.IsCancellationRequested)
                    {
                        msg.SetCanceled();
                        continue;
                    }

                    _retryQueue.Enqueue(msg);
                }

                // shifting retry queue
                for (var i = 0; i < count; i++)
                {
                    var msg = _retryQueue.Dequeue();

                    if (msg.Cancellation.IsCancellationRequested)
                    {
                        msg.SetCanceled();
                        continue;
                    }

                    _retryQueue.Enqueue(msg);
                }
            }
        }

        private async Task SendMessageAsync(MessageHolder msg, CancellationToken cancellation)
        {
            await _channel.InvokeActionAsync(model =>
            {
                lock (_syncQueue)
                {
                    msg.Sequence = model.NextPublishSeqNo;

                    var properties = model.CreateBasicProperties();
                    msg.Properties.CopyTo(properties);

                    model.BasicPublish(_exchage.Name, msg.PublishProperties.RoutingKey ?? "",
                        msg.PublishProperties.Mandatory ?? false, properties,
                        msg.Body);

                    if (ConfirmsMode)
                    {
                        _ackQueue.Enqueue(msg);
                    }
                }
            }, cancellation)
                .ConfigureAwait(false);
        }

        private async Task<bool> SendRetryQueueAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested && _retryQueue.Any())
            {
                var msg = _retryQueue.Peek();

                if (msg == null)
                    // no messages, exitting
                    break;

                msg.Processing = true;
                if (msg.Cancellation.IsCancellationRequested)
                {
                    msg.SetCanceled();

                    // removing message
                    _retryQueue.Dequeue();
                    continue;
                }

                try
                {
                    await SendMessageAsync(msg, cancellation)
                        .ConfigureAwait(false);

                    // all ok, removing message
                    _retryQueue.Dequeue();
                    if (!ConfirmsMode)
                    {
                        msg.SetResult();
                    }

                    msg.Processing = false;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Cannot publish message: {ex.Message}");

                    msg.Processing = false;

                    lock (_syncQueue)
                    {
                        if (_channel.IsOpen)
                        {
                            _topology.ScheduleConfiguration(true);
                        }
                        else
                        {
                            RequeueUnacked();
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        private async Task<bool> SendPublishQueueAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                MessageHolder msg;

                try
                {
                    msg = await _publishQueue.DequeueAsync(_loopCancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                msg.Processing = true;
                if (msg.Cancellation.IsCancellationRequested)
                {
                    msg.SetCanceled();
                    msg.Processing = false;
                    continue;
                }

                try
                {
                    await SendMessageAsync(msg, cancellation)
                        .ConfigureAwait(false);

                    if (!ConfirmsMode)
                    {
                        msg.SetResult();
                    }

                    msg.Processing = false;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Cannot publish message: {ex.Message}");

                    lock (_syncQueue)
                    {
                        if (_channel.IsOpen)
                        {
                            if (msg.Cancellation.IsCancellationRequested)
                            {
                                msg.SetCanceled();
                            }
                            else
                            {
                                _retryQueue.Enqueue(msg);
                            }

                            _topology.ScheduleConfiguration(true);
                        }
                        else
                        {
                            _retryQueue.Enqueue(msg);
                            RequeueUnacked();
                        }
                        msg.Processing = false;
                    }

                    return false;
                }
            }

            return true;
        }

        private async Task SendLoopAsync()
        {
            if (ConfirmsMode)
            {
                await _channel.InvokeActionAsync(model => model.ConfirmSelect(), _loopCancellation)
                    .ConfigureAwait(false);
            }

            if (!await SendRetryQueueAsync(_loopCancellation).ConfigureAwait(false))
            {
                if (_loopCancellation.IsCancellationRequested)
                {
                    RequeueUnacked();
                }

                return;
            }

            if (_loopCancellation.IsCancellationRequested)
            {
                RequeueUnacked();
            }

            await SendPublishQueueAsync(_loopCancellation).ConfigureAwait(false);

            RequeueUnacked();
        }

        #endregion

        #region Channel handlers

        private void ChannelOnReturn(object sender, BasicReturnEventArgs e)
        {
            lock (_syncQueue)
            {
                if (_ackQueue.Any())
                {
                    var msg = _ackQueue.Dequeue();
                    msg.SetException(new LinkMessageReturnedException(e.ReplyText));
                }
            }
        }

        private void ChannelOnNack(object sender, BasicNackEventArgs e)
        {
            lock (_syncQueue)
            {
                while (_ackQueue.Any() && _ackQueue.Peek()?.Sequence <= e.DeliveryTag)
                {
                    var msg = _ackQueue.Dequeue();
                    msg.SetException(new LinkMessageNackedException());
                }
            }
        }

        private void ChannelOnAck(object sender, BasicAckEventArgs e)
        {
            lock (_syncQueue)
            {
                while (_ackQueue.Any() && _ackQueue.Peek()?.Sequence <= e.DeliveryTag)
                {
                    var msg = _ackQueue.Dequeue();
                    msg.SetResult();
                }
            }
        }

        #endregion

        #region Topology handlers       

        private async Task TopologyConfigureAsync(ILinkTopologyConfig config)
        {
            _exchage = await Task.Run(async () => await _topologyConfigHandler(config)
                .ConfigureAwait(false), _disposedCancellation)
                .ConfigureAwait(false);
        }

        private Task TopologyReadyAsync()
        {
            // ReSharper disable once MethodSupportsCancellation
            return Task.Run(() =>
            {
                lock (_sync)
                {
                    _logger.Debug("Topology ready");
                    _loopCancellationSource?.Cancel();
                    _loopCancellationSource?.Dispose();
                    // ReSharper disable once MethodSupportsCancellation                    
                    _loopTask?.WaitWithoutException();
                    _loopTask?.Dispose();

                    _loopCancellationSource = new CancellationTokenSource();
                    _loopCancellation = _disposedCancellationSource.Token;
                    _loopTask = Task.Run(async () => await SendLoopAsync().ConfigureAwait(false), _loopCancellation);
                }
            });
        }

        private Task TopologyConfigurationErrorAsync(Exception ex)
        {
            // ReSharper disable once MethodSupportsCancellation
            return Task.Run(async () =>
            {
                _logger.Warning("Cannot configure topology for producer: {0}", ex.Message);
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