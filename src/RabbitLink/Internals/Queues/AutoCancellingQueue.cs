#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Async;

#endregion

namespace RabbitLink.Internals.Queues
{
    class AutoCancellingQueue<TItem> where TItem:IWorkQueueItem
    {
        #region Fields

        private readonly LinkedList<QueueItem> _queue = new LinkedList<QueueItem>();
        private readonly AsyncLock _sync = new AsyncLock();

        #endregion

        /// <summary>
        ///     Takes first <see cref="WorkItem{TValue,TResult}" /> from queue
        /// </summary>
        /// <param name="cancellationToken">token to cancel operation</param>
        /// <returns>First <see cref="WorkItem{TValue,TResult}" /> or null if queue empty</returns>
        public TItem Take(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (_sync.Lock(cancellationToken))
                {
                    var node = _queue.First;
                    if (node == null)
                    {
                        return default(TItem);
                    }

                    node.List.Remove(node);
                    node.Value.DisableCancellation();

                    var item = node.Value.Value;
                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    return item;
                }
            }
        }

        /// <summary>
        ///     Asynchronously takes first <see cref="WorkItem{TValue,TResult}" /> from queue
        /// </summary>
        /// <param name="cancellationToken">token to cancel operation</param>
        /// <returns>Tasks which will be completes with first <see cref="WorkItem{TValue,TResult}" /> or null if queue empty</returns>
        public async Task<TItem> TakeAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (await _sync.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    var node = _queue.First;
                    if (node == null)
                    {
                        return default(TItem);
                    }

                    node.List.Remove(node);
                    node.Value.DisableCancellation();

                    var item = node.Value.Value;
                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    return item;
                }
            }
        }

        public void Put(TItem item, CancellationToken cancellationToken)
        {
            using (_sync.Lock(cancellationToken))
            {
                var qitem = new QueueItem(item);
                var node = _queue.AddLast(qitem);

                qitem.EnableCancellation(async () =>
                {
                    using (await _sync.LockAsync(CancellationToken.None).ConfigureAwait(false))
                    {
                        node.List?.Remove(node);
                    }
                });
            }
        }

        public async Task PutAsync(TItem item, CancellationToken cancellationToken)
        {
            using (await _sync.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                var qitem = new QueueItem(item);
                var node = _queue.AddLast(qitem);

                qitem.EnableCancellation(async () =>
                {
                    using (await _sync.LockAsync(CancellationToken.None).ConfigureAwait(false))
                    {
                        node.List?.Remove(node);
                    }
                });
            }
        }

        public void PutRetry(IEnumerable<TItem> items,
            CancellationToken cancellationToken)
        {
            using (_sync.Lock(cancellationToken))
            {
                foreach (var item in items)
                {
                    var qitem = new QueueItem(item);
                    var node = _queue.AddLast(qitem);

                    qitem.EnableCancellation(async () =>
                    {
                        using (await _sync.LockAsync(CancellationToken.None).ConfigureAwait(false))
                        {
                            node.List?.Remove(node);
                        }
                    });
                }
            }
        }

        public async Task PutRetryAsync(IEnumerable<TItem> items,
            CancellationToken cancellationToken)
        {
            using (await _sync.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var item in items)
                {
                    var qitem = new QueueItem(item);
                    var node = _queue.AddLast(qitem);

                    qitem.EnableCancellation(async () =>
                    {
                        using (await _sync.LockAsync(CancellationToken.None).ConfigureAwait(false))
                        {
                            node.List?.Remove(node);
                        }
                    });
                }
            }
        }

        #region Nested types

        #region QueueItem

        /// <summary>
        ///     Class to store <see cref="WorkItem{TValue,TResult}" /> with it cancellation
        /// </summary>
        private class QueueItem
        {
            #region Fields

            private readonly object _cancellationSync = new object();
            private CancellationTokenRegistration? _cancellationRegistration;
            private CancellationTokenSource _cancellationSource;

            #endregion

            #region Ctor

            public QueueItem(TItem value)
            {
                Value = value;
            }

            #endregion

            #region Properties

            public TItem Value { get; }

            #endregion

            public void EnableCancellation(Func<Task> cancelAction)
            {
                if (_cancellationRegistration != null)
                    throw new InvalidOperationException("Cancellation already enabled");

                lock (_cancellationSync)
                {
                    if (_cancellationRegistration != null)
                        throw new InvalidOperationException("Cancellation already enabled");

                    _cancellationSource = new CancellationTokenSource();

                    _cancellationRegistration =
                        Value.Cancellation.Register(() =>
                        {
                            Value.TrySetCanceled(Value.Cancellation);
                            Task.Run(async ()=>
                            {
                                var ret = cancelAction?.Invoke();

                                if (ret != null)
                                {
                                    await ret.ConfigureAwait(false);
                                }
                            }, _cancellationSource.Token);
                        });
                }
            }

            public void DisableCancellation()
            {
                if (_cancellationRegistration == null)
                    return;

                lock (_cancellationSync)
                {
                    if (_cancellationRegistration == null)
                        return;

                    _cancellationSource?.Cancel();
                    _cancellationSource?.Dispose();
                    _cancellationSource = null;

                    _cancellationRegistration?.Dispose();
                    _cancellationRegistration = null;
                }
            }
        }

        #endregion

        #endregion
    }
}