#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Async;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal class CompositeWorkQueue<TItem> where TItem : IWorkQueueItem
    {
        #region Fields

        private readonly ConcurrentWorkQueue<TItem> _queue =
            new ConcurrentWorkQueue<TItem>();

        private readonly AsyncLock _sync = new AsyncLock();


        private readonly AutoCancellingQueue<TItem> _tempQueue =
            new AutoCancellingQueue<TItem>();

        #endregion

        #region Properties

        public bool AddingCompleted { get; private set; }

        #endregion

        public void Complete()
        {
            if (AddingCompleted)
                return;

            using (_sync.Lock(CancellationToken.None))
            {
                if (AddingCompleted)
                    return;

                _queue.Complete();
                AddingCompleted = true;
            }
        }

        public void Complete(Action<TItem> completeAction)
        {
            if (completeAction == null)
                throw new ArgumentNullException(nameof(completeAction));

            if (AddingCompleted)
                return;

            using (_sync.Lock(CancellationToken.None))
            {
                if (AddingCompleted)
                    return;

                _queue.Complete();
                while (true)
                {
                    TItem item;
                    try
                    {
                        item = _tempQueue.Take(CancellationToken.None);
                        if (item == null)
                        {
                            item = _queue.Wait(CancellationToken.None);
                        }
                    }
                    catch
                    {
                        AddingCompleted = true;
                        return;
                    }

                    completeAction(item);
                }
            }
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
            if (AddingCompleted)
                throw new InvalidOperationException("Adding already completed");

            using (_sync.Lock(cancellation))
            {
                if (AddingCompleted)
                    throw new InvalidOperationException("Adding already completed");

                _tempQueue.PutRetry(items, cancellation);
            }
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