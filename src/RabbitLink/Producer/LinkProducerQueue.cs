#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Async;
using RabbitLink.Internals;

#endregion

namespace RabbitLink.Producer
{
    internal class LinkProducerQueue : LinkQueue<LinkProducerQueueMessage>
    {
        private readonly LinkedList<LinkProducerQueueMessage> _retryQueue =
            new LinkedList<LinkProducerQueueMessage>();

        private readonly AsyncLock _retryQueueLock = new AsyncLock();

        protected override void OnDispose()
        {
            var ex = new ObjectDisposedException(GetType().Name);

            using (_retryQueueLock.Lock())
            {
                while (_retryQueue.Count > 0)
                {
                    _retryQueue.Last.Value.DisableCancellationAsync().WaitWithoutException();
                    Interlocked.MemoryBarrier();
                    _retryQueue.Last.Value.SetException(ex);
                    _retryQueue.RemoveLast();
                }
            }
        }

        public async Task EnqueueRetryAsync(LinkProducerQueueMessage message, bool prepend = false)
        {
            if (DisposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (message.Cancellation.IsCancellationRequested)
            {
                message.SetCancelled();
                return;
            }

            using (await _retryQueueLock.LockAsync().ConfigureAwait(false))
            {
                if (DisposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (message.Cancellation.IsCancellationRequested)
                {
                    message.SetCancelled();
                    return;
                }
                
                if (prepend)
                {
                    _retryQueue.AddLast(message);
                }
                else
                {
                    _retryQueue.AddFirst(message);
                }
                await message.EnableCancellationAsync().ConfigureAwait(false);
            }
        }

        public async Task EnqueueRetryAsync(IEnumerable<LinkProducerQueueMessage> messages, bool prepend = false)
        {
            if (DisposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            using (await _retryQueueLock.LockAsync().ConfigureAwait(false))
            {
                if (DisposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (prepend)
                {
                    var lastItem = _retryQueue.Last;
                    foreach (var message in messages)
                    {
                        if (message.Cancellation.IsCancellationRequested)
                        {
                            message.SetCancelled();
                            continue;
                        }

                        _retryQueue.AddAfter(lastItem, message);
                        await message.EnableCancellationAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    foreach (var message in messages)
                    {
                        if (message.Cancellation.IsCancellationRequested)
                        {
                            message.SetCancelled();
                            continue;
                        }

                        _retryQueue.AddFirst(message);
                        await message.EnableCancellationAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        protected override async Task<LinkProducerQueueMessage> OnDequeueAsync(CancellationToken cancellation)
        {
            using (await _retryQueueLock.LockAsync(cancellation).ConfigureAwait(false))
            {
                if (_retryQueue.Count > 0)
                {
                    var ret = _retryQueue.Last.Value;
                    _retryQueue.RemoveLast();
                    return ret;
                }
            }

            return null;
        }
    }
}