#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

#endregion

namespace RabbitLink.Producer
{
    internal class LinkProducerQueue : IDisposable
    {
        private readonly CancellationToken _disposedCancellation;
        private readonly CancellationTokenSource _disposedCancellationSource = new CancellationTokenSource();

        private readonly LinkedList<LinkProducerQueueMessage> _retryQueue =
            new LinkedList<LinkProducerQueueMessage>();

        private readonly AsyncProducerConsumerQueue<LinkProducerQueueMessage> _sendQueue =
            new AsyncProducerConsumerQueue<LinkProducerQueueMessage>();

        public LinkProducerQueue()
        {
            _disposedCancellation = _disposedCancellationSource.Token;
        }

        public void Dispose()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            lock (_disposedCancellationSource)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _disposedCancellationSource.Cancel();

                _sendQueue.CompleteAdding();
                Interlocked.MemoryBarrier();

                var ex = new ObjectDisposedException(GetType().Name);

                foreach (var message in _sendQueue.GetConsumingEnumerable())
                {
                    message.DisableCancellation();
                    message.SetException(ex);
                }

                lock (_retryQueue)
                {
                    while (_retryQueue.Count > 0)
                    {
                        _retryQueue.Last.Value.DisableCancellation();
                        _retryQueue.Last.Value.SetException(ex);
                        _retryQueue.RemoveLast();
                    }
                }

                _disposedCancellationSource.Dispose();
            }
        }

        public async Task EnqueueAsync(LinkProducerQueueMessage message)
        {
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(0)
                .ConfigureAwait(false);

            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            message.EnableCancellation();

            await _sendQueue.EnqueueAsync(message, message.Cancellation);
        }

        public void EnqueueRetry(LinkProducerQueueMessage message, bool prepend = false)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            lock (_retryQueue)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (prepend)
                {
                    _retryQueue.AddLast(message);
                }
                else
                {
                    _retryQueue.AddFirst(message);
                }
            }
        }

        public void EnqueueRetry(IEnumerable<LinkProducerQueueMessage> messages, bool prepend = false)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            lock (_retryQueue)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (prepend)
                {
                    var lastItem = _retryQueue.Last;
                    foreach (var message in messages)
                    {
                        _retryQueue.AddAfter(lastItem, message);
                    }
                }
                else
                {
                    foreach (var message in messages)
                    {
                        _retryQueue.AddFirst(message);
                    }
                }
            }
        }

        public async Task<LinkProducerQueueMessage> DequeueAsync(CancellationToken cancellation)
        {
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(0)
                .ConfigureAwait(false);

            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            using (
                var compositeCancelaltionSource = CancellationTokenHelpers.Normalize(_disposedCancellation, cancellation)
                )
            {
                LinkProducerQueueMessage message;

                while (!compositeCancelaltionSource.Token.IsCancellationRequested)
                {
                    lock (_retryQueue)
                    {
                        if (_retryQueue.Count == 0)
                            break;

                        message = _retryQueue.Last.Value;
                        _retryQueue.RemoveLast();
                    }

                    message.DisableCancellation();
                    Interlocked.MemoryBarrier();
                    if (!message.Cancellation.IsCancellationRequested)
                    {
                        return message;
                    }
                }

                while (!compositeCancelaltionSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        message = await _sendQueue.DequeueAsync(compositeCancelaltionSource.Token)
                            .ConfigureAwait(false);
                    }
                    catch (InvalidOperationException)
                    {
                        // Queue complete adding and empty
                        break;
                    }

                    message.DisableCancellation();
                    Interlocked.MemoryBarrier();
                    if (!message.Cancellation.IsCancellationRequested)
                    {
                        return message;
                    }
                }
            }

            cancellation.ThrowIfCancellationRequested();
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}