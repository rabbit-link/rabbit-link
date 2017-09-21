using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Async;
using RabbitLink.Internals.Queues;

namespace RabbitLink.Internals.Channels
{
    internal class CompositeChannel<T> : ICompositeChannel<T>
        where T : class, IChannelItem
    {
        private readonly RetryQueue<T> _retryQueue = new RetryQueue<T>();
        private readonly IChannel<T> _channel;
        private readonly AsyncLock _sync = new AsyncLock();

        public CompositeChannel(IChannel<T> channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public void Dispose()
        {
            _channel.Dispose();
            _retryQueue.Dispose();
        }

        public void Put(T item)
            => _channel.Put(item);

        public Task PutAsync(T item)
            => _channel.PutAsync(item);

        public T Wait(CancellationToken cancellation)
        {
            using (_sync.Lock(cancellation))
            {
                var item = _retryQueue.Take(cancellation);

                if (item != null)
                    return item;

                return _channel.Wait(cancellation);
            }
        }

        public async Task<T> WaitAsync(CancellationToken cancellation)
        {
            using (await _sync.LockAsync(cancellation).ConfigureAwait(false))
            {
                var item = await _retryQueue.TakeAsync(cancellation)
                    .ConfigureAwait(false);

                if (item != null)
                    return item;

                return await _channel.WaitAsync(cancellation)
                    .ConfigureAwait(false);
            }
        }

        public T Spin(CancellationToken cancellation)
        {
            using (_sync.Lock(cancellation))
            {
                var item = _retryQueue.Take(cancellation);

                if (item != null)
                    return item;

                return _channel.Spin(cancellation);
            }
        }

        public async Task<T> SpinAsync(CancellationToken cancellation)
        {
            using (await _sync.LockAsync(cancellation).ConfigureAwait(false))
            {
                var item = await _retryQueue.TakeAsync(cancellation)
                    .ConfigureAwait(false);

                if (item != null)
                    return item;

                return await _channel.SpinAsync(cancellation)
                    .ConfigureAwait(false);
            }
        }

        public void PutRetry(IEnumerable<T> items, CancellationToken cancellation)
        {
            using (_sync.Lock(cancellation))
            {
                _retryQueue.PutRetry(items, cancellation);
            }
        }

        public async Task PutRetryAsync(IEnumerable<T> items, CancellationToken cancellation)
        {
            using (await _sync.LockAsync(cancellation).ConfigureAwait(false))
            {
                await _retryQueue.PutRetryAsync(items, cancellation)
                    .ConfigureAwait(false);
            }
        }

        public void Yield(CancellationToken cancellation)
        {
            while (true)
            {
                using (_sync.Lock(cancellation))
                {
                    var item = _channel.Spin(cancellation);

                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    _retryQueue.Put(item, CancellationToken.None);
                }
            }
        }

        public async Task YieldAsync(CancellationToken cancellation)
        {
            while (true)
            {
                using (await _sync.LockAsync(cancellation).ConfigureAwait(false))
                {
                    var item = await _channel.SpinAsync(cancellation)
                        .ConfigureAwait(false);

                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    await _retryQueue.PutAsync(item, CancellationToken.None);
                }
            }
        }
    }
}
