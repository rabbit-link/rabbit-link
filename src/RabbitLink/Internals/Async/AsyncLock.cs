#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Async
{
    internal class AsyncLock
    {
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(1, 1);

        private IDisposable GetUnlockingDisposable()
        {
            return new OnceDisposable(() =>
            {
                _sem.Release();
                Interlocked.MemoryBarrier();
            });
        }

        public Task<IDisposable> LockAsync()
        {
            return LockAsync(CancellationToken.None);
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            await _sem.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            Interlocked.MemoryBarrier();

            return GetUnlockingDisposable();
        }

        public IDisposable Lock()
        {
            return Lock(CancellationToken.None);
        }

        public IDisposable Lock(CancellationToken cancellationToken)
        {
            _sem.Wait(cancellationToken);
            return GetUnlockingDisposable();
        }
    }
}