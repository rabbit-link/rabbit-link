#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

#endregion

namespace RabbitLink.Internals
{
    internal abstract class LinkAbstractQueueMessage
    {
        private readonly object _cancellationRegistrationSync = new object();
        private readonly TaskCompletionSource _completion = new TaskCompletionSource();
        private CancellationTokenRegistration? _cancellationRegistration;

        public LinkAbstractQueueMessage(CancellationToken cancellation)
        {            
            Cancellation = cancellation;
        }

        public CancellationToken Cancellation { get; }        
        public Task Task => _completion.Task;

        public void SetResult()
        {
            _completion.TrySetResult();
        }

        public void SetException(Exception exception)
        {
            _completion.TrySetException(exception);
        }

        public void SetCancelled()
        {
            _completion.TrySetCanceled();
        }

        public void EnableCancellation()
        {
            lock (_cancellationRegistrationSync)
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

        public void DisableCancellation()
        {
            lock (_cancellationRegistrationSync)
            {
                if (_cancellationRegistration == null)
                    return;

                _cancellationRegistration?.Dispose();
                _cancellationRegistration = null;
            }
        }
    }
}