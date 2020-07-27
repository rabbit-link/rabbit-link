using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Async;
using RabbitLink.Internals.Channels;

namespace RabbitLink.Internals.Lens
{
    internal class LensChannel<T> : IChannel<T> where T : class, IChannelItem
    {
        private readonly SemaphoreSlim _writeSem = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _readSem = new SemaphoreSlim(0, 1);
        private long _writersCount, _readersCount;

        private readonly object _sync = new object();

        private readonly CancellationTokenSource _disposedCancellationSource =
            new CancellationTokenSource();

        private readonly CancellationToken _disposedCancellation;

        private T _item;

        public LensChannel()
        {
            _disposedCancellation = _disposedCancellationSource.Token;
        }

        public void Put(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            Interlocked.MemoryBarrier();
            Interlocked.Increment(ref _writersCount);
            Interlocked.MemoryBarrier();

            try
            {
                using (var compositeCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, item.Cancellation))
                {
                    try
                    {
                        _writeSem.Wait(compositeCancellation.Token);
                    }
                    catch
                    {
                        if (_disposedCancellation.IsCancellationRequested)
                            throw new ObjectDisposedException(GetType().Name);

                        throw;
                    }
                }

                Interlocked.MemoryBarrier();
                _item = item;
                Interlocked.MemoryBarrier();

                _readSem.Release();
            }
            finally
            {
                Interlocked.MemoryBarrier();
                Interlocked.Decrement(ref _writersCount);
            }
        }

        public async Task PutAsync(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);


            Interlocked.MemoryBarrier();
            Interlocked.Increment(ref _writersCount);
            Interlocked.MemoryBarrier();

            try
            {
                using (var compositeCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, item.Cancellation))
                {
                    try
                    {
                        await _writeSem.WaitAsync(compositeCancellation.Token)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        if (_disposedCancellation.IsCancellationRequested)
                            throw new ObjectDisposedException(GetType().Name);

                        throw;
                    }
                }

                Interlocked.MemoryBarrier();
                _item = item;
                Interlocked.MemoryBarrier();

                _readSem.Release();
            }
            finally
            {
                Interlocked.MemoryBarrier();
                Interlocked.Decrement(ref _writersCount);
            }
        }

        public T Wait(CancellationToken cancellation)
        {
            using var compositeCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(_disposedCancellation, cancellation);

            while (true)
            {
                if (_disposedCancellation.IsCancellationRequested)
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
                    catch
                    {
                        if (_disposedCancellation.IsCancellationRequested)
                            throw new ObjectDisposedException(GetType().Name);

                        throw;
                    }

                    try
                    {
                        if (_item == null)
                            continue;

                        if (_item.Cancellation.IsCancellationRequested)
                        {
                            _item.TrySetCanceled(_item.Cancellation);
                            continue;
                        }

                        return _item;
                    }
                    finally
                    {
                        _item = null;
                        _writeSem.Release();
                    }
                }
                finally
                {
                    Interlocked.MemoryBarrier();
                    Interlocked.Decrement(ref _readersCount);
                }
            }
        }

        public async Task<T> WaitAsync(CancellationToken cancellation)
        {
            using var compositeCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(_disposedCancellation, cancellation);

            while (true)
            {
                if (_disposedCancellation.IsCancellationRequested)
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
                    catch
                    {
                        if (_disposedCancellation.IsCancellationRequested)
                            throw new ObjectDisposedException(GetType().Name);

                        throw;
                    }

                    try
                    {
                        if (_item == null)
                            continue;

                        if (_item.Cancellation.IsCancellationRequested)
                        {
                            _item.TrySetCanceled(_item.Cancellation);
                            continue;
                        }

                        return _item;
                    }
                    finally
                    {
                        _item = null;
                        _writeSem.Release();
                    }
                }
                finally
                {
                    Interlocked.MemoryBarrier();
                    Interlocked.Decrement(ref _readersCount);
                }
            }
        }

        public T Spin(CancellationToken cancellation)
        {
            using var compositeCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(_disposedCancellation, cancellation);

            while (true)
            {
                if (_disposedCancellation.IsCancellationRequested)
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
                    catch
                    {
                        if (_disposedCancellation.IsCancellationRequested)
                            throw new ObjectDisposedException(GetType().Name);

                        throw;
                    }

                    if (_item == null)
                    {
                        _writeSem.Release();
                        continue;
                    }

                    if (_item.Cancellation.IsCancellationRequested)
                    {
                        _item.TrySetCanceled(_item.Cancellation);
                        _item = null;
                        _writeSem.Release();
                        continue;
                    }
                }
                finally
                {
                    Interlocked.MemoryBarrier();
                    Interlocked.Decrement(ref _readersCount);
                }

                _readSem.Release();

                Task.Delay(100, compositeCancellation.Token)
                    .WaitWithoutException();
            }
        }

        public async Task<T> SpinAsync(CancellationToken cancellation)
        {
            using var compositeCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(_disposedCancellation, cancellation);

            while (true)
            {
                if (_disposedCancellation.IsCancellationRequested)
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
                    catch
                    {
                        if (_disposedCancellation.IsCancellationRequested)
                            throw new ObjectDisposedException(GetType().Name);

                        throw;
                    }

                    if (_item == null)
                    {
                        _writeSem.Release();
                        continue;
                    }

                    if (_item.Cancellation.IsCancellationRequested)
                    {
                        _item.TrySetCanceled(_item.Cancellation);
                        _item = null;
                        _writeSem.Release();
                        continue;
                    }
                }
                finally
                {
                    Interlocked.MemoryBarrier();
                    Interlocked.Decrement(ref _readersCount);
                }

                _readSem.Release();

                try
                {
                    await Task.Delay(100, compositeCancellation.Token)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // no-op
                }
            }
        }

        public void Dispose()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _disposedCancellationSource.Cancel();


                SpinWait.SpinUntil(() =>
                    Interlocked.Read(ref _writersCount) == 0 &&
                    Interlocked.Read(ref _readersCount) == 0
                );

                if (_item != null)
                {
                    try
                    {
                        if (_item.Cancellation.IsCancellationRequested)
                        {
                            _item.TrySetCanceled(_item.Cancellation);
                        }
                        else
                        {
                            _item.TrySetException(new ObjectDisposedException(GetType().Name));
                        }
                        _item = null;
                    }
                    catch
                    {
                        // No-op
                    }
                }

                _disposedCancellationSource.Dispose();
                _readSem.Dispose();
                _writeSem.Dispose();
            }
        }
    }
}
