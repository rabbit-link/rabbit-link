#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

#endregion

namespace RabbitLink.Internals
{
    internal class LinkQueueMessage : LinkAbstractQueueMessage
    {
        private readonly TaskCompletionSource _completion = new TaskCompletionSource();

        public LinkQueueMessage(CancellationToken cancellation) : base(cancellation)
        {
        }

        public override void SetException(Exception exception)
        {
            _completion.TrySetExceptionWithBackgroundContinuations(exception);
        }

        public override void SetCancelled()
        {
            _completion.TrySetCanceledWithBackgroundContinuations();
        }

        public void SetResult()
        {
            _completion.TrySetResultWithBackgroundContinuations();
        }

        public Task Task => _completion.Task;
    }

    internal class LinkQueueMessage<TResult> : LinkAbstractQueueMessage
    {
        private readonly TaskCompletionSource<TResult> _completion = new TaskCompletionSource<TResult>();

        public LinkQueueMessage(CancellationToken cancellation) : base(cancellation)
        {
        }

        public override void SetException(Exception exception)
        {
            _completion.TrySetExceptionWithBackgroundContinuations(exception);
        }

        public override void SetCancelled()
        {
            _completion.TrySetCanceledWithBackgroundContinuations();
        }

        public void SetResult(TResult result)
        {            
            _completion.TrySetResultWithBackgroundContinuations(result);
        }

        public Task<TResult> Task => _completion.Task;
    }

    internal abstract class LinkAbstractQueueMessage
    {
        private readonly AsyncLock _cancellationRegistrationSync = new AsyncLock();
        private CancellationTokenRegistration? _cancellationRegistration;

        public LinkAbstractQueueMessage(CancellationToken cancellation)
        {
            Cancellation = cancellation;            
        }
        
        public CancellationToken Cancellation { get; }                

        public abstract void SetException(Exception exception);
        public abstract void SetCancelled();

        public async Task EnableCancellationAsync()
        {
            using(await _cancellationRegistrationSync.LockAsync().ConfigureAwait(false))
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
            using(await _cancellationRegistrationSync.LockAsync().ConfigureAwait(false))
            {
                if (_cancellationRegistration == null)
                    return;

                _cancellationRegistration?.Dispose();
                _cancellationRegistration = null;
            }
        }
    }
}