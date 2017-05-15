#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Async;

#endregion

namespace RabbitLink.Internals.Queues
{
    class WorkQueue<TItem> where TItem:IWorkQueueItem
    {
        #region Fields

        private readonly ConcurrentWorkQueue<TItem> _queue =
            new ConcurrentWorkQueue<TItem>();

        private readonly AsyncLock _sync = new AsyncLock();

        private readonly AutoCancellingQueue<TItem> _tempQueue =
            new AutoCancellingQueue<TItem>();

        #endregion

        public void Complete()
        {
            _queue.CompleteAdding();
        }

        public void Complete(Action<TItem> completeAction)
        {
            if (completeAction == null)
                throw new ArgumentNullException(nameof(completeAction));

            _queue.CompleteAdding();
            while (true)
            {
                TItem item;
                try
                {
                    item = Wait(CancellationToken.None);
                }
                catch
                {
                    return;
                }

                completeAction(item);
            }
        }

        public void Yield(CancellationToken cancellation)
        {
            while (true)
            {
                using (_sync.Lock(cancellation))
                {
                    var item = _queue.Wait(cancellation);
                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    _tempQueue.Put(item, CancellationToken.None);
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public async Task YieldAsync(CancellationToken cancellation)
        {
            while (true)
            {
                using (await _sync.LockAsync(cancellation).ConfigureAwait(false))
                {
                    var item = await _queue.WaitAsync(cancellation)
                        .ConfigureAwait(false);

                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    await _tempQueue.PutAsync(item, CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Put(TItem item)
        {
            _queue.Put(item);
        }

        public void PutRetry(IEnumerable<TItem> items, CancellationToken cancellation)
        {
            _tempQueue.PutRetry(items, cancellation);
        }

        public Task PutRetryAsync(IEnumerable<TItem> items, CancellationToken cancellation)
        {
            return _tempQueue.PutRetryAsync(items, cancellation);
        }

        public TItem Wait(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (_sync.Lock(cancellationToken))
                {
                    var item = _tempQueue.Take(cancellationToken);
                    if (item != null)
                    {
                        return item;
                    }

                    return _queue.Wait(cancellationToken);
                }
            }
        }

        public async Task<TItem> WaitAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (await _sync.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = await _tempQueue.TakeAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (item != null)
                    {
                        return item;
                    }

                    return await _queue.WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}