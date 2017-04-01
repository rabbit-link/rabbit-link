#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Async;

#endregion

namespace RabbitLink.Internals
{
    internal abstract class LinkAbstractQueueMessage
    {
        private readonly AsyncLock _cancellationRegistrationSync = new AsyncLock();
        private CancellationTokenRegistration? _cancellationRegistration;

        protected LinkAbstractQueueMessage(CancellationToken cancellation)
        {
            Cancellation = cancellation;
        }

        public CancellationToken Cancellation { get; }

        public abstract void SetException(Exception exception);
        public abstract void SetCancelled();

        public async Task EnableCancellationAsync()
        {
            using (await _cancellationRegistrationSync.LockAsync().ConfigureAwait(false))
            {
                if (_cancellationRegistration != null)
                    return;

                try
                {
                    _cancellationRegistration = Cancellation.Register(SetCancelled);
                }
                catch (ObjectDisposedException)
                {
                    // Cancellation source was disposed
                    if (Cancellation.IsCancellationRequested)
                    {
                        SetCancelled();
                    }
                    _cancellationRegistration = new CancellationTokenRegistration();
                }
            }
        }

        public async Task DisableCancellationAsync()
        {
            using (await _cancellationRegistrationSync.LockAsync().ConfigureAwait(false))
            {
                if (_cancellationRegistration == null)
                    return;

                _cancellationRegistration?.Dispose();
                _cancellationRegistration = null;
            }
        }
    }
}