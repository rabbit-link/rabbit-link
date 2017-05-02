#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Async;

#endregion

namespace RabbitLink.Internals.Queues
{
    class YieldingWorkQueue<TValue, TResult>
    {
        #region Fields

        private readonly ConcurrentWorkQueue<TValue, TResult> _inQueue = new ConcurrentWorkQueue<TValue, TResult>();

        private readonly AsyncLock _sync = new AsyncLock();

        private readonly AutoCancellingWorkQueue<TValue, TResult> _tempQueue =
            new AutoCancellingWorkQueue<TValue, TResult>();

        #endregion

        public void CompleteAdding()
        {
            _inQueue.CompleteAdding();
        }


        public Task<TResult> PutAsync(TValue value, CancellationToken cancellationToken)
        {
            return _inQueue.PutAsync(value, cancellationToken);
        }

        public void Put(WorkItem<TValue, TResult> item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _inQueue.Put(item);
        }


        public async Task<WorkItem<TValue, TResult>> WaitAsync(CancellationToken cancellationToken)
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

                    return await _inQueue.WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        public WorkItem<TValue, TResult> Wait(CancellationToken cancellationToken)
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

                    return _inQueue.Wait(cancellationToken);
                }
            }
        }


        public async Task YieldAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (await _sync.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = await _inQueue.WaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.Completion.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    await _tempQueue.PutAsync(item, CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }


        public void Yield(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (_sync.Lock(cancellationToken))
                {
                    var item = _inQueue.Wait(cancellationToken);
                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.Completion.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    _tempQueue.Put(item, CancellationToken.None);
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }
    }
}