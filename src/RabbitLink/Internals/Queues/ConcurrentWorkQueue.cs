#region Usings

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal class ConcurrentWorkQueue<TItem> where TItem : IWorkQueueItem
    {
        #region Fields

        private readonly CancellationTokenSource _addingCompleteSource = new CancellationTokenSource();
        private readonly object _addingCompleteSync = new object();
        private readonly CancellationToken _addingCompleteToken;

        private readonly ConcurrentQueue<QueueItem> _queue =
            new ConcurrentQueue<QueueItem>();

        private readonly SemaphoreSlim _readSemaphore = new SemaphoreSlim(0);

        private long _addingCompleted;
        private long _writeCounter;

        #endregion

        #region Ctor

        public ConcurrentWorkQueue()
        {
            _addingCompleteToken = _addingCompleteSource.Token;
        }

        #endregion

        public void Put(TItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Cancellation.IsCancellationRequested)
            {
                item.TrySetCanceled(item.Cancellation);
                return;
            }

            if (Interlocked.Read(ref _addingCompleted) != 0)
                throw new InvalidOperationException("Adding already completed");

            Interlocked.Increment(ref _writeCounter);

            try
            {
                if (Interlocked.Read(ref _addingCompleted) != 0)
                    throw new InvalidOperationException("Adding already completed");

                var qitem = new QueueItem(item);

                qitem.EnableCancellation();
                _queue.Enqueue(qitem);
                _readSemaphore.Release();
            }
            finally
            {
                Interlocked.Decrement(ref _writeCounter);
            }
        }

        public TItem Wait(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!_addingCompleteToken.IsCancellationRequested)
                {
                    using (var compositeCancellation = CancellationTokenSource
                        .CreateLinkedTokenSource(_addingCompleteToken, cancellationToken))
                    {
                        try
                        {
                            _readSemaphore.Wait(compositeCancellation.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            if (!_addingCompleteToken.IsCancellationRequested)
                                throw;
                        }
                    }
                }

                QueueItem qitem;
                if (!_queue.TryDequeue(out qitem))
                {
                    throw new InvalidOperationException("Adding completed and queue is empty");
                }

                qitem.DisableCancellation();

                var item = qitem.Value;

                if (item.Cancellation.IsCancellationRequested)
                {
                    item.TrySetCanceled(item.Cancellation);
                    continue;
                }

                return item;
            }
        }

        public void Complete()
        {
            if (_addingCompleteToken.IsCancellationRequested)
                return;

            lock (_addingCompleteSync)
            {
                if (_addingCompleteToken.IsCancellationRequested)
                    return;

                var wait = new SpinWait();
                Interlocked.CompareExchange(ref _addingCompleted, 1, 0);

                while (true)
                {
                    if (Interlocked.Read(ref _writeCounter) == 0)
                    {
                        break;
                    }
                    wait.SpinOnce();
                }

                _addingCompleteSource.Cancel();
                _addingCompleteSource.Dispose();
            }
        }

        public async Task<TItem> WaitAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!_addingCompleteToken.IsCancellationRequested)
                {
                    using (var compositeCancellation = CancellationTokenSource
                        .CreateLinkedTokenSource(_addingCompleteToken, cancellationToken))
                    {
                        try
                        {
                            await _readSemaphore.WaitAsync(compositeCancellation.Token)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            if (!_addingCompleteToken.IsCancellationRequested)
                                throw;
                        }
                    }
                }

                QueueItem qitem;
                if (!_queue.TryDequeue(out qitem))
                {
                    throw new InvalidOperationException("Adding completed and queue is empty");
                }

                qitem.DisableCancellation();
                var item = qitem.Value;

                if (item.Cancellation.IsCancellationRequested)
                {
                    item.TrySetCanceled(item.Cancellation);
                    continue;
                }

                return item;
            }
        }

        #region Nested types

        #region QueueItem

        private class QueueItem
        {
            #region Fields

            private readonly object _cancellationSync = new object();
            private CancellationTokenRegistration? _cancellationRegistration;

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

            public void EnableCancellation()
            {
                if (_cancellationRegistration != null)
                    throw new InvalidOperationException("Cancellation already enabled");

                lock (_cancellationSync)
                {
                    if (_cancellationRegistration != null)
                        throw new InvalidOperationException("Cancellation already enabled");

                    _cancellationRegistration =
                        Value.Cancellation.Register(() => Value.TrySetCanceled(Value.Cancellation));
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

                    _cancellationRegistration?.Dispose();
                    _cancellationRegistration = null;
                }
            }
        }

        #endregion

        #endregion
    }
}