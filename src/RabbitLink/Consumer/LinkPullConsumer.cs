#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Messaging;

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

        #endregion

        #region Ctor

        public LinkPullConsumer(ILinkConsumerBuilder consumerBuilder, TimeSpan getMessageTimeout)
        {
            if (consumerBuilder == null)
                throw new ArgumentNullException(nameof(consumerBuilder));

            if (getMessageTimeout < TimeSpan.Zero && getMessageTimeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(getMessageTimeout), "Must be greater or equal zero or equal Timeout.InfiniteTimeSpan");

            GetMessageTimeout = getMessageTimeout;

            _consumer = consumerBuilder
                .ErrorStrategy(new LinkConsumerDefaultErrorStrategy())
                .Handler<byte[]>(OnMessageRecieved)
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

        public async Task<ILinkPulledMessage<byte[]>> GetMessageAsync(CancellationToken? cancellation = null)
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

        private Task OnMessageRecieved(ILinkConsumedMessage<byte[]> message)
        {
            return _queue.PutAsync(message);
        }

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