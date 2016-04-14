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

        public async Task<QueueMessage> GetMessageAsync(CancellationToken cancellation)
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

        public void Enqueue(ILinkRecievedMessage<byte[]> msg, Action<CancellationToken> onAck,
            Action<CancellationToken, bool> onNack)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            var cancellation = _messageCancellation;

            try
            {
                Action ackHandler = () =>
                {
                    if (cancellation.IsCancellationRequested)
                        return;

                    onAck(_messageCancellation);
                };

                Action<bool> nackHandler = requeue =>
                {
                    if (cancellation.IsCancellationRequested)
                        return;

                    onNack(_messageCancellation, requeue);
                };

                var holder = new MessageHolder(new QueueMessage(msg, ackHandler, nackHandler), cancellation);
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

        internal class QueueMessage
        {
            public QueueMessage(ILinkRecievedMessage<byte[]> message, Action ack, Action<bool> nack)
            {
                Message = message;
                Ack = ack;
                Nack = nack;
            }

            public Action Ack { get; }
            public Action<bool> Nack { get; }
            public ILinkRecievedMessage<byte[]> Message { get; }
        }

        #region Private classes

        private class MessageHolder
        {
            public MessageHolder(QueueMessage message, CancellationToken cancellation)
            {
                Message = message;
                Cancellation = cancellation;
            }

            public QueueMessage Message { get; }
            public CancellationToken Cancellation { get; }
        }

        private class HandlerHolder
        {
            private readonly TaskCompletionSource<QueueMessage> _completion = new TaskCompletionSource<QueueMessage>();
            private readonly IDisposable _cancellationRegistration;

            public HandlerHolder(CancellationToken cancellation)
            {
                _cancellationRegistration = cancellation.Register(OnCancellationCancelled);
            }

            private void OnCancellationCancelled()
            {
                _completion.SetCanceled();
            }

            public bool SetMessage(QueueMessage message)
            {
                var ret = _completion.TrySetResult(message);
                _cancellationRegistration.Dispose();
                return ret;
            }

            public void SetException(Exception exception)
            {
                _completion.TrySetException(exception);
            }

            public Task<QueueMessage> Task => _completion.Task;
        }

        #endregion
    }
}