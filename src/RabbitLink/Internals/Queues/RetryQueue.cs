#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Async;
using RabbitLink.Internals.Channels;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal class RetryQueue<TItem> : IDisposable
        where TItem: class, IChannelItem
    {
        private readonly LinkedList<QueueItem> _queue = new LinkedList<QueueItem>();
        private readonly AsyncLock _queueSync = new AsyncLock();
        private readonly AsyncLock _sync = new AsyncLock();
        private readonly CancellationTokenSource _disposedCancellationSource = new CancellationTokenSource();
        private readonly CancellationToken _disposedCancellation;

        public RetryQueue()
        {
            _disposedCancellation = _disposedCancellationSource.Token;
        }

        /// <summary>
        ///     Takes first <see cref="ChannelItem{TValue,TResult}" /> from queue
        /// </summary>
        /// <param name="cancellation">token to cancel operation</param>
        /// <returns>First <see cref="ChannelItem{TValue,TResult}" /> or null if queue empty</returns>
        public TItem Take(CancellationToken cancellation)
        {
            while (true)
            {
                if(_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    using (var compositeCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, cancellation))
                    {
                        using (_sync.Lock(compositeCancellation.Token))
                        {
                            QueueItem item;

                            using (_queueSync.Lock(compositeCancellation.Token))
                            {
                                var node = _queue.First;
                                if (node == null)
                                {
                                    return null;
                                }

                                node.List.Remove(node);
                                item = node.Value;
                            }

                            item.DisableCancellation();
                            var ret = item.Value;

                            if (ret.Cancellation.IsCancellationRequested)
                            {
                                ret.TrySetCanceled(ret.Cancellation);
                                continue;
                            }

                            return ret;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if(_disposedCancellation.IsCancellationRequested)
                        throw new ObjectDisposedException(GetType().Name);

                    throw;
                }
            }
        }

        /// <summary>
        ///     Asynchronously takes first <see cref="ChannelItem{TValue,TResult}" /> from queue
        /// </summary>
        /// <param name="cancellation">token to cancel operation</param>
        /// <returns>Tasks which will be completes with first <see cref="ChannelItem{TValue,TResult}" /> or null if queue empty</returns>
        public async Task<TItem> TakeAsync(CancellationToken cancellation)
        {
            while (true)
            {
                if(_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    using (var compositeCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, cancellation))
                    {
                        using (await  _sync.LockAsync(compositeCancellation.Token).ConfigureAwait(false))
                        {
                            QueueItem item;

                            using (await _queueSync.LockAsync(compositeCancellation.Token).ConfigureAwait(false))
                            {
                                var node = _queue.First;
                                if (node == null)
                                {
                                    return null;
                                }

                                node.List.Remove(node);
                                item = node.Value;
                            }

                            item.DisableCancellation();
                            var ret = item.Value;

                            if (ret.Cancellation.IsCancellationRequested)
                            {
                                ret.TrySetCanceled(ret.Cancellation);
                                continue;
                            }

                            return ret;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if(_disposedCancellation.IsCancellationRequested)
                        throw new ObjectDisposedException(GetType().Name);

                    throw;
                }
            }
        }

        public void Put(TItem item, CancellationToken cancellation)
        {
            if(_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            try
            {
                using (var compositeCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, cancellation))
                {
                    using (_sync.Lock(compositeCancellation.Token))
                    {
                        LinkedListNode<QueueItem> node;
                        var qitem = new QueueItem(item);

                        using (_queueSync.Lock(cancellation))
                        {
                            node = _queue.AddLast(qitem);
                        }

                        qitem.EnableCancellation(() =>
                        {
                            using (_queueSync.Lock(CancellationToken.None))
                            {
                                node.List?.Remove(node);
                            }
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if(_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                throw;
            }
        }

        public async Task PutAsync(TItem item, CancellationToken cancellation)
        {
            if(_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            try
            {
                using (var compositeCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, cancellation))
                {
                    using (await _sync.LockAsync(compositeCancellation.Token).ConfigureAwait(false))
                    {
                        LinkedListNode<QueueItem> node;
                        var qitem = new QueueItem(item);

                        using (await _queueSync.LockAsync(cancellation).ConfigureAwait(false))
                        {
                            node = _queue.AddLast(qitem);
                        }

                        qitem.EnableCancellation(() =>
                        {
                            using (_queueSync.Lock(CancellationToken.None))
                            {
                                node.List?.Remove(node);
                            }
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if(_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                throw;
            }
        }

        public void PutRetry(IEnumerable<TItem> items,
            CancellationToken cancellation)
        {
            if(_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);
            
            var enableCancellations = new Stack<Action>();
            LinkedListNode<QueueItem> prevNode = null;


            try
            {
                using (var compositeCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, cancellation))
                {
                    using (_sync.Lock(compositeCancellation.Token))
                    {
                        using (_queueSync.Lock(compositeCancellation.Token))
                        {
                            foreach (var item in items)
                            {
                                if (item.Cancellation.IsCancellationRequested)
                                {
                                    item.TrySetCanceled(item.Cancellation);
                                    continue;
                                }

                                var qitem = new QueueItem(item);

                                var node = prevNode == null
                                    ? _queue.AddFirst(qitem)
                                    : _queue.AddAfter(prevNode, qitem);

                                enableCancellations.Push(() =>
                                {
                                    qitem.EnableCancellation(() =>
                                    {
                                        using (_queueSync.Lock(CancellationToken.None))
                                        {
                                            node.List?.Remove(node);
                                        }
                                    });
                                });

                                prevNode = node;
                            }
                        }

                        while (enableCancellations.Count > 0)
                        {
                            var a = enableCancellations.Pop();
                            a();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if(_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                throw;
            }
        }
        
        public void Dispose()
        {
            if(_disposedCancellation.IsCancellationRequested)
                return;

            using (_sync.Lock(CancellationToken.None))
            {
                if(_disposedCancellation.IsCancellationRequested)
                    return;
                
                _disposedCancellationSource.Cancel();

                
                var itemException = new ObjectDisposedException(GetType().Name);

                while (true)
                {
                    QueueItem item;

                    using (_queueSync.Lock(CancellationToken.None))
                    {
                        var qitem = _queue.First;
                        if(qitem == null)
                            break;
                        
                        _queue.RemoveFirst();
                        item = qitem.Value;
                    }

                    try
                    {
                        item.DisableCancellation();

                        if (item.Value.Cancellation.IsCancellationRequested)
                        {
                            item.Value.TrySetCanceled(item.Value.Cancellation);
                        }
                        else
                        {
                            item.Value.TrySetException(itemException);
                        }
                    }
                    catch
                    {
                        // no-op
                    }
                }
                
                _disposedCancellationSource.Dispose();
            }
        }

        #region Nested types

        #region QueueItem

        /// <summary>
        ///     Class to store <see cref="ChannelItem{TValue,TResult}" /> with it cancellation
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

            public void EnableCancellation(Action cancelAction)
            {
                if (_cancellationRegistration != null)
                    throw new InvalidOperationException("Cancellation already enabled");

                lock (_cancellationSync)
                {
                    if (_cancellationRegistration != null)
                        throw new InvalidOperationException("Cancellation already enabled");

                    _cancellationSource = new CancellationTokenSource();

                    _cancellationRegistration = Value
                        .Cancellation
                        .Register(() =>
                        {
                            Value.TrySetCanceled(Value.Cancellation);
                            cancelAction?.Invoke();
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