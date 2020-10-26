#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkPullConsumer : ILinkPullConsumer
    {
        #region Fields

        private readonly ILinkConsumer _consumer;
        private readonly LinkPullConsumerQueue _queue = new LinkPullConsumerQueue();
        private readonly object _sync = new object();
        private bool _disposed;
        private readonly LinkTypeNameMapping _typeNameMapping;
        private readonly ILinkSerializer _serializer;

        #endregion

        #region Ctor

        public LinkPullConsumer(
            ILinkConsumerBuilder consumerBuilder,
            TimeSpan getMessageTimeout,
            LinkTypeNameMapping typeNameMapping,
            ILinkSerializer serializer
        )
        {
            if (consumerBuilder == null)
                throw new ArgumentNullException(nameof(consumerBuilder));

            if (getMessageTimeout < TimeSpan.Zero && getMessageTimeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(getMessageTimeout),
                    "Must be greater or equal zero or equal Timeout.InfiniteTimeSpan");

            GetMessageTimeout = getMessageTimeout;
            _typeNameMapping = typeNameMapping ?? throw new ArgumentNullException(nameof(typeNameMapping));
            _serializer = serializer;

            _consumer = consumerBuilder
                .ErrorStrategy(new LinkConsumerDefaultErrorStrategy())
                .Handler(OnMessageReceived)
                .OnStateChange(OnStateChanged)
                .Build();
        }

        #endregion

        #region ILinkPullConsumer Members

        public void Dispose()
            => _consumer.Dispose();

        public Guid Id => _consumer.Id;
        public ushort PrefetchCount => _consumer.PrefetchCount;
        public bool AutoAck => _consumer.AutoAck;
        public int Priority => _consumer.Priority;
        public bool CancelOnHaFailover => _consumer.CancelOnHaFailover;
        public bool Exclusive => _consumer.Exclusive;

        public Task WaitReadyAsync(CancellationToken? cancellation = null)
            => _consumer.WaitReadyAsync(cancellation);

        public TimeSpan GetMessageTimeout { get; }


        public async Task<ILinkPulledMessage<TBody>> GetMessageAsync<TBody>(CancellationToken? cancellation = null)
            where TBody : class
        {
            if (typeof(TBody) != typeof(byte[]) && _serializer == null)
                throw new InvalidOperationException("Serializer not set");

            if (typeof(TBody) == typeof(object) && _typeNameMapping.IsEmpty)
                throw new InvalidOperationException("Type name mapping is empty");

            while (true)
            {
                var msg = await GetRawMessageAsync(cancellation)
                    .ConfigureAwait(false);

                if (typeof(TBody) == typeof(byte[]))
                    return (ILinkPulledMessage<TBody>) msg;

                try
                {
                    Type bodyType;
                    if (typeof(TBody) == typeof(object))
                    {
                        var typeName = msg.Properties.Type;
                        if (string.IsNullOrWhiteSpace(typeName))
                            throw new LinkPullConsumerTypeNameMappingException(msg);

                        bodyType = _typeNameMapping.Map(typeName!.Trim());
                        if (bodyType == null)
                            throw new LinkPullConsumerTypeNameMappingException(msg, typeName!);
                    }
                    else
                    {
                        bodyType = typeof(TBody);
                    }

                    TBody body;
                    var props = msg.Properties.Clone();

                    try
                    {
                        body = (TBody) _serializer.Deserialize(bodyType, msg.Body, props);
                    }
                    catch (Exception ex)
                    {
                        throw new LinkPullConsumerDeserializationException(msg, bodyType, ex);
                    }

                    var concreteMsg = LinkMessageFactory
                        .ConstructPulledMessage(bodyType, msg, body, props);

                    return (ILinkPulledMessage<TBody>) concreteMsg;
                }
                catch (Exception ex)
                {
                    msg.Exception(ex);
                }
            }
        }

        private async Task<LinkPulledMessage<byte[]>> GetRawMessageAsync(CancellationToken? cancellation = null)
        {
            if (cancellation == null)
            {
                if (GetMessageTimeout == TimeSpan.Zero || GetMessageTimeout == Timeout.InfiniteTimeSpan)
                {
                    return await _queue.TakeAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                }

                using (var cs = new CancellationTokenSource(GetMessageTimeout))
                {
                    return await _queue.TakeAsync(cs.Token)
                        .ConfigureAwait(false);
                }
            }

            return await _queue.TakeAsync(cancellation.Value)
                .ConfigureAwait(false);
        }

        #endregion

        private void OnStateChanged(LinkConsumerState oldState, LinkConsumerState newsState)
        {
            if (newsState == LinkConsumerState.Disposed)
            {
                OnDispose();
            }
        }

        private Task<LinkConsumerAckStrategy> OnMessageReceived(ILinkConsumedMessage<byte[]> message)
            => _queue.PutAsync(message);

        private void OnDispose()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                if (_disposed)
                    return;

                _queue.Dispose();
                _disposed = true;
            }
        }
    }
}
