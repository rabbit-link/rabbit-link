#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkConsumerMessageQueue : IDisposable
    {
        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly CancellationToken _disposedCancellation;

        private readonly AsyncProducerConsumerQueue<HandlerHolder> _handlersQueue =
            new AsyncProducerConsumerQueue<HandlerHolder>();

        private readonly AsyncProducerConsumerQueue<MessageHolder> _messageQueue =
            new AsyncProducerConsumerQueue<MessageHolder>();

        private readonly Task _loopTask;

        private CancellationTokenSource _messageCancellationSource;
        private CancellationToken _messageCancellation;

        public LinkConsumerMessageQueue()
        {
            _disposedCancellationSource = new CancellationTokenSource();
            _disposedCancellation = _disposedCancellationSource.Token;

            _messageCancellationSource = new CancellationTokenSource();
            _messageCancellation = _messageCancellationSource.Token;

            _loopTask = Task.Run(async () => await LoopAsync().ConfigureAwait(false));
        }

        public void Dispose()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            _disposedCancellationSource.Cancel();
            _disposedCancellationSource.Dispose();

            _messageCancellationSource.Cancel();
            _messageCancellationSource.Dispose();

            _messageQueue.CompleteAdding();
            _handlersQueue.CompleteAdding();

            // ReSharper disable once MethodSupportsCancellation
            _loopTask.WaitWithoutException();
            _loopTask.Dispose();

            foreach (var handlerHolder in _handlersQueue.GetConsumingEnumerable())
            {
                handlerHolder.SetException(new ObjectDisposedException(GetType().Name));
            }

            _handlersQueue.Dispose();
            _messageQueue.Dispose();
        }

        #region Loop

        private async Task LoopAsync()
        {
            while (!_disposedCancellation.IsCancellationRequested)
            {
                MessageHolder messageHolder;

                try
                {
                    messageHolder = await _messageQueue.DequeueAsync(_disposedCancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                using (var compositeCancellation = CancellationTokenHelpers
                    .Normalize(_disposedCancellation, messageHolder.Cancellation))
                {
                    var cancellationToken = compositeCancellation.Token;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        HandlerHolder handlerHolder;
                        try
                        {
                            handlerHolder = await _handlersQueue.DequeueAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            // message cancelled
                            continue;
                        }

                        if (handlerHolder.SetMessage(messageHolder.Message))
                        {
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        public async Task<LinkMessage<byte[]>> GetMessageAsync(CancellationToken cancellation)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            var holder = new HandlerHolder(cancellation);

            using (var compositeCancellation = CancellationTokenHelpers
                .Normalize(_disposedCancellation, cancellation))
            {
                try
                {
                    await _handlersQueue.EnqueueAsync(holder, compositeCancellation.Token)
                        .ConfigureAwait(false);
                }
                catch
                {
                    if (_disposedCancellation.IsCancellationRequested)
                        throw new ObjectDisposedException(GetType().Name);

                    throw;
                }
            }

            return await holder.Task
                .ConfigureAwait(false);
        }

        public void Enqueue(byte[] body, LinkMessageProperties properties, LinkRecieveMessageProperties recieveProperties, LinkMessageOnAckAsyncDelegate onAck,
            LinkMessageOnNackAsyncDelegate onNack)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            var cancellation = _messageCancellation;

            try
            {
                LinkMessageOnAckAsyncDelegate ackHandler = async token =>
                {
                    using (var compositeCancellation = CancellationTokenHelpers.Normalize(cancellation, token))
                    {
                        await onAck(compositeCancellation.Token)
                            .ConfigureAwait(false);
                    }
                };

                LinkMessageOnNackAsyncDelegate nackHandler = async (requeue, token) =>
                {
                    using (var compositeCancellation = CancellationTokenHelpers.Normalize(cancellation, token))
                    {
                        await onNack(requeue, compositeCancellation.Token)
                            .ConfigureAwait(false);
                    }
                };

                var message = new LinkMessage<byte[]>(body, properties, recieveProperties, ackHandler, nackHandler);
                var holder = new MessageHolder(message, cancellation);
                _messageQueue.Enqueue(holder, cancellation);
            }
            catch
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void CancelMessages()
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            var oldCancellation = _messageCancellationSource;
            _messageCancellationSource = new CancellationTokenSource();
            _messageCancellation = _messageCancellationSource.Token;

            oldCancellation.Cancel();
            oldCancellation.Dispose();
        }

        #region Private classes

        private class MessageHolder
        {
            public MessageHolder(LinkMessage<byte[]> message, CancellationToken cancellation)
            {
                Message = message;
                Cancellation = cancellation;
            }

            public LinkMessage<byte[]> Message { get; }
            public CancellationToken Cancellation { get; }
        }

        private class HandlerHolder
        {
            private readonly TaskCompletionSource<LinkMessage<byte[]>> _completion = new TaskCompletionSource<LinkMessage<byte[]>>();
            private readonly IDisposable _cancellationRegistration;

            public HandlerHolder(CancellationToken cancellation)
            {
                _cancellationRegistration = cancellation.Register(OnCancellationCancelled);
            }

            private void OnCancellationCancelled()
            {
                _completion.TrySetCanceled();
            }

            public bool SetMessage(LinkMessage<byte[]> message)
            {
                var ret = _completion.TrySetResult(message);
                _cancellationRegistration.Dispose();
                return ret;
            }

            public void SetException(Exception exception)
            {
                _completion.TrySetException(exception);
            }

            public Task<LinkMessage<byte[]>> Task => _completion.Task;
        }

        #endregion
    }
}