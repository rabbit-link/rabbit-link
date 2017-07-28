#region Usings

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Channels;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal class QueueChannel<TItem> : IChannel<TItem>
        where TItem : class, IChannelItem
    {
        #region Fields

        private readonly CancellationTokenSource _disposeCancellationSource = new CancellationTokenSource();
        private readonly object _sync = new object();
        private readonly CancellationToken _disposeCancellation;

        private readonly ConcurrentQueue<QueueItem> _queue =
            new ConcurrentQueue<QueueItem>();

        private readonly SemaphoreSlim _readSem = new SemaphoreSlim(0);

        private long _writersCount, _readersCount;

        #endregion

        #region Ctor

        public QueueChannel()
        {
            _disposeCancellation = _disposeCancellationSource.Token;
        }

        #endregion

        public void Put(TItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            item.Cancellation.ThrowIfCancellationRequested();

            if (_disposeCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            Interlocked.MemoryBarrier();
            Interlocked.Increment(ref _writersCount);
            Interlocked.MemoryBarrier();

            try
            {
                if (_disposeCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                var qitem = new QueueItem(item);

                qitem.EnableCancellation();
                _queue.Enqueue(qitem);
                _readSem.Release();
            }
            finally
            {
                Interlocked.MemoryBarrier();
                Interlocked.Decrement(ref _writersCount);
            }
        }

        public Task PutAsync(TItem item)
        {
            Put(item);
            return Task.CompletedTask;
        }

        public TItem Wait(CancellationToken cancellationToken)
        {
            using (var compositeCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(_disposeCancellation, cancellationToken))
            {
                while (true)
                {
                    if (_disposeCancellation.IsCancellationRequested)
                        throw new ObjectDisposedException(GetType().Name);

                    Interlocked.MemoryBarrier();
                    Interlocked.Increment(ref _readersCount);
                    Interlocked.MemoryBarrier();

                    try
                    {

                        try
                        {
                            _readSem.Wait(compositeCancellation.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            if (_disposeCancellation.IsCancellationRequested)
                                throw new ObjectDisposedException(GetType().Name);

                            throw;
                        }


                        QueueItem qitem;
                        if (!_queue.TryDequeue(out qitem))
                            continue;

                        qitem.DisableCancellation();

                        var item = qitem.Value;
                        if (item.Cancellation.IsCancellationRequested)
                        {
                            item.TrySetCanceled(item.Cancellation);
                            continue;
                        }

                        return item;
                    }
                    finally
                    {
                        Interlocked.MemoryBarrier();
                        Interlocked.Decrement(ref _readersCount);
                    }
                }
            }
        }

        public async Task<TItem> WaitAsync(CancellationToken cancellationToken)
        {
            using (var compositeCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(_disposeCancellation, cancellationToken))
            {
                while (true)
                {
                    if (_disposeCancellation.IsCancellationRequested)
                        throw new ObjectDisposedException(GetType().Name);

                    Interlocked.MemoryBarrier();
                    Interlocked.Increment(ref _readersCount);
                    Interlocked.MemoryBarrier();

                    try
                    {

                        try
                        {
                            await _readSem.WaitAsync(compositeCancellation.Token)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            if (_disposeCancellation.IsCancellationRequested)
                                throw new ObjectDisposedException(GetType().Name);

                            throw;
                        }


                        QueueItem qitem;
                        if (!_queue.TryDequeue(out qitem))
                            continue;

                        qitem.DisableCancellation();

                        var item = qitem.Value;
                        if (item.Cancellation.IsCancellationRequested)
                        {
                            item.TrySetCanceled(item.Cancellation);
                            continue;
                        }

                        return item;
                    }
                    finally
                    {
                        Interlocked.MemoryBarrier();
                        Interlocked.Decrement(ref _readersCount);
                    }
                }
            }
        }

        public TItem Spin(CancellationToken cancellation)
            => Wait(cancellation);

        public Task<TItem> SpinAsync(CancellationToken cancellation)
            => WaitAsync(cancellation);

        public void Dispose()
        {
            if (_disposeCancellation.IsCancellationRequested)
                return;

            lock (_sync)
            {
                if (_disposeCancellation.IsCancellationRequested)
                    return;

                _disposeCancellationSource.Cancel();

                SpinWait.SpinUntil(() =>
                    Interlocked.Read(ref _writersCount) == 0 &&
                    Interlocked.Read(ref _readersCount) == 0
                );

                var itemException = new ObjectDisposedException(GetType().Name);
                QueueItem qitem;
                while (_queue.TryDequeue(out qitem))
                {
                    try
                    {
                        qitem.DisableCancellation();

                        if (qitem.Value.Cancellation.IsCancellationRequested)
                        {
                            qitem.Value.TrySetCanceled(qitem.Value.Cancellation);
                        }
                        else
                        {
                            qitem.Value.TrySetException(itemException);
                        }
                    }
                    catch
                    {
                        // no-op
                    }
                }

                _disposeCancellationSource.Dispose();
                _readSem.Dispose();
            }
        }

        private class QueueItem
        {
            private readonly object _cancellationSync = new object();
            private CancellationTokenRegistration? _cancellationRegistration;

            public QueueItem(TItem value)
            {
                Value = value;
            }

            public TItem Value { get; }

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
    }
}