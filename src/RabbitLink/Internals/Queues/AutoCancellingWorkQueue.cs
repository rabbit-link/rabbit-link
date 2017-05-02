#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Async;

#endregion

namespace RabbitLink.Internals.Queues
{
    class AutoCancellingWorkQueue<TValue, TResult>
    {
        #region Fields

        private readonly LinkedList<QueueItem> _queue = new LinkedList<QueueItem>();
        private readonly AsyncLock _sync = new AsyncLock();

        #endregion

        /// <summary>
        /// Takes first <see cref="WorkItem{TValue,TResult}"/> from queue
        /// </summary>
        /// <param name="cancellationToken">token to cancel operation</param>
        /// <returns>First <see cref="WorkItem{TValue,TResult}"/> or null if queue empty</returns>
        public WorkItem<TValue, TResult> Take(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (_sync.Lock(cancellationToken))
                {
                    var node = _queue.First;
                    if (node == null)
                    {
                        return null;
                    }

                    node.List.Remove(node);
                    node.Value.DisableCancellation();

                    var item = node.Value.Value;
                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.Completion.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    return item;
                }
            }
        }

        /// <summary>
        /// Asynchronously takes first <see cref="WorkItem{TValue,TResult}"/> from queue
        /// </summary>
        /// <param name="cancellationToken">token to cancel operation</param>
        /// <returns>Tasks which will be completes with first <see cref="WorkItem{TValue,TResult}"/> or null if queue empty</returns>
        public async Task<WorkItem<TValue, TResult>> TakeAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                using (await _sync.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    var node = _queue.First;
                    if (node == null)
                    {
                        return null;
                    }

                    node.List.Remove(node);
                    node.Value.DisableCancellation();

                    var item = node.Value.Value;
                    if (item.Cancellation.IsCancellationRequested)
                    {
                        item.Completion.TrySetCanceled(item.Cancellation);
                        continue;
                    }

                    return item;
                }
            }
        }

        public void Put(WorkItem<TValue, TResult> item, CancellationToken cancellationToken)
        {
            var qitem = new QueueItem(item);

            using (_sync.Lock(cancellationToken))
            {
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

        public async Task PutAsync(WorkItem<TValue, TResult> item, CancellationToken cancellationToken)
        {
            var qitem = new QueueItem(item);

            using (await _sync.LockAsync(cancellationToken).ConfigureAwait(false))
            {
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

        #region Nested types

        #region QueueItem

        /// <summary>
        /// Class to store <see cref="WorkItem{TValue,TResult}"/> with it cancellation
        /// </summary>
        private class QueueItem
        {
            #region Fields

            private readonly object _cancellationSync = new object();
            private CancellationTokenRegistration? _cancellationRegistration;
            private CancellationTokenSource _cancellationSource;

            #endregion

            #region Ctor

            public QueueItem(WorkItem<TValue, TResult> value)
            {
                Value = value;
            }

            #endregion

            #region Properties

            public WorkItem<TValue, TResult> Value { get; }

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

                    Value.Completion.Task.ContinueWith(async _ =>
                        {
                            var ret = cancelAction?.Invoke();

                            if (ret != null)
                            {
                                await ret.ConfigureAwait(false);
                            }
                        }, _cancellationSource.Token,
                        TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);

                    _cancellationRegistration =
                        Value.Cancellation.Register(() => Value.Completion.TrySetCanceled(Value.Cancellation));
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