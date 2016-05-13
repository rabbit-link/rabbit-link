#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;

#endregion

namespace RabbitLink.Internals
{


    internal class LinkQueue<TMessage> : IDisposable where TMessage : LinkAbstractQueueMessage
    {
        private readonly CancellationTokenSource _disposedCancellationSource = new CancellationTokenSource();

        private readonly AsyncProducerConsumerQueue<TMessage> _queue =
            new AsyncProducerConsumerQueue<TMessage>();

        public LinkQueue()
        {
            DisposedCancellation = _disposedCancellationSource.Token;
        }

        protected CancellationToken DisposedCancellation { get; }

        public void Dispose()
        {
            if (DisposedCancellation.IsCancellationRequested)
                return;

            lock (_disposedCancellationSource)
            {
                if (DisposedCancellation.IsCancellationRequested)
                    return;

                _disposedCancellationSource.Cancel();

                Interlocked.MemoryBarrier();
                var ex = new ObjectDisposedException(GetType().Name);

                _queue.CompleteAdding();
                foreach (var message in _queue.GetConsumingEnumerable())
                {
                    // ReSharper disable once MethodSupportsCancellation
                    message.DisableCancellationAsync().WaitWithoutException();
                    message.SetException(ex);
                }
                _queue.Dispose();

                OnDispose();                
                _disposedCancellationSource.Dispose();
            }
        }

        protected virtual void OnDispose()
        {
        }

        public async Task EnqueueAsync(TMessage message)
        {
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(0)
                .ConfigureAwait(false);

            if (DisposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);
            
            await message.EnableCancellationAsync().ConfigureAwait(false);

            try
            {
                await _queue.EnqueueAsync(message, message.Cancellation);
            }
            catch (InvalidOperationException)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

#pragma warning disable 1998
        protected virtual async Task<TMessage> OnDequeueAsync(CancellationToken cancellation)
        {
            return null;
        }
#pragma warning restore 1998

        public async Task<TMessage> DequeueAsync(CancellationToken cancellation)
        {
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(0)
                .ConfigureAwait(false);

            if (DisposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            using (
                var compositeCancelaltionSource = CancellationTokenHelpers.Normalize(DisposedCancellation, cancellation)
                )
            {
                TMessage message;

                while (!compositeCancelaltionSource.Token.IsCancellationRequested)
                {
                    message = await OnDequeueAsync(compositeCancelaltionSource.Token)
                        .ConfigureAwait(false);

                    if (message == null)
                        break;

                    await message.DisableCancellationAsync().ConfigureAwait(false);
                    Interlocked.MemoryBarrier();
                    if (!message.Cancellation.IsCancellationRequested)

                    {
                        return message;                        
                    }

                    message.SetCancelled();
                }

                while (!compositeCancelaltionSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        message = await _queue.DequeueAsync(compositeCancelaltionSource.Token)
                            .ConfigureAwait(false);
                    }
                    catch (InvalidOperationException)
                    {
                        // Queue complete adding and empty
                        break;
                    }

                    await message.DisableCancellationAsync().ConfigureAwait(false);
                    Interlocked.MemoryBarrier();
                    if (!message.Cancellation.IsCancellationRequested)
                    {
                        return message;
                    }

                    message.SetCancelled();
                }
            }

            cancellation.ThrowIfCancellationRequested();
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}