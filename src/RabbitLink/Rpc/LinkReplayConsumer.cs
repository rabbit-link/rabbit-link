using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Consumer;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;
using RabbitLink.Serialization;

namespace RabbitLink.Rpc
{
    /// <summary>
    /// Rpc client consumer realization
    /// </summary>
    public class LinkReplayConsumer : ILinkConsumer
    {
        private readonly ILinkConsumer _consumer;

        private readonly CorrelationDictonary _subscriptions;

        private readonly ILinkSerializer _serializer;


        internal LinkReplayConsumer(ILinkConsumer consumer, CorrelationDictonary subscriptions, ILinkSerializer serializer)
        {
            _consumer = consumer;
            _subscriptions = subscriptions;
            _serializer = serializer;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _consumer.Dispose();
            while (!_subscriptions.IsEmpty)
            {
                var first = _subscriptions.FirstOrDefault().Key;
                if(first == null) continue;
                if (_subscriptions.TryRemove(first, out var source))
                {
                    source.TrySetCanceled();
                }
            }
        }

        /// <inheritdoc />
        public Guid Id => _consumer.Id;

        /// <inheritdoc />
        public ushort PrefetchCount => _consumer.PrefetchCount;

        /// <inheritdoc />
        public bool AutoAck => _consumer.AutoAck;

        /// <inheritdoc />
        public int Priority => _consumer.Priority;

        /// <inheritdoc />
        public bool CancelOnHaFailover => _consumer.CancelOnHaFailover;

        /// <inheritdoc />
        public bool Exclusive => _consumer.Exclusive;

        /// <inheritdoc />
        public Task WaitReadyAsync(CancellationToken? cancellation = null)
            => _consumer.WaitReadyAsync(cancellation);



        internal async Task<ILinkConsumedMessage<byte[]>> Subscribe(string correlationId,
            CancellationToken cancellation = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(correlationId));
            await WaitReadyAsync(cancellation);
            var source = new TaskCompletionSource<ILinkConsumedMessage<byte[]>>();
            if(!_subscriptions.TryAdd(correlationId, source))
                throw new ArgumentException($"CorrelationId {correlationId} already awaited");
            cancellation.Register(() =>
            {
                _subscriptions.TryRemove(correlationId, out var _);
                source.TrySetCanceled(cancellation);
            });
            return await source.Task;
        }

        internal async Task<ILinkConsumedMessage<TResponse>> Subscribe<TResponse>(string correlationId,
            CancellationToken cancellation = default(CancellationToken))
            where TResponse : class
        {
            if(_serializer == null)
                throw new InvalidOperationException("Cannot make typed subscription without serializer");
            var answer = await Subscribe(correlationId, cancellation);
            var deserialized = _serializer.Deserialize<TResponse>(answer.Body, answer.Properties);
            return new LinkConsumedMessage<TResponse>(deserialized, answer.Properties, answer.RecieveProperties,
                answer.Cancellation);
        }


        /// <inheritdoc />
        ~LinkReplayConsumer()
        {
            Dispose();
        }
    }


}
