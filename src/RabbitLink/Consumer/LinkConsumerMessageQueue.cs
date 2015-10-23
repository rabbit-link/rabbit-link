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
        private readonly CancellationTokenSource _disposedCancellation = new CancellationTokenSource();

        private readonly AsyncProducerConsumerQueue<HandlerHolder> _handlersQueue =
            new AsyncProducerConsumerQueue<HandlerHolder>();

        private readonly Task _loopTask;

        private readonly AsyncProducerConsumerQueue<MessageHolder> _messageQueue =
            new AsyncProducerConsumerQueue<MessageHolder>();

        private CancellationTokenSource _messageCancellation =
            new CancellationTokenSource();

        public LinkConsumerMessageQueue()
        {
            _loopTask = Task.Run(async () => await Loop().ConfigureAwait(false));
        }

        public void Dispose()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            _disposedCancellation.Cancel();
            _messageCancellation.Cancel();
            _messageQueue.CompleteAdding();
            _handlersQueue.CompleteAdding();

            _loopTask.WaitWithoutException();

            foreach (var handlerHolder in _handlersQueue.GetConsumingEnumerable())
            {
                handlerHolder.Completion.TrySetException(new ObjectDisposedException(GetType().Name));
            }

            _handlersQueue.Dispose();
            _messageQueue.Dispose();
        }

        #region Loop

        private async Task Loop()
        {
            while (!_disposedCancellation.IsCancellationRequested)
            {
                MessageHolder messageHolder;

                try
                {
                    messageHolder = await _messageQueue.DequeueAsync(_disposedCancellation.Token)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                var compositeCancellation = CancellationTokenHelpers
                    .Normalize(_disposedCancellation.Token, messageHolder.Cancellation);

                while (!compositeCancellation.Token.IsCancellationRequested)
                {
                    HandlerHolder handlerHolder;
                    try
                    {
                        handlerHolder = await _handlersQueue.DequeueAsync(compositeCancellation.Token)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // message cancelled
                        continue;
                    }

                    if (handlerHolder.Completion.TrySetResult(messageHolder.Message))
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        public async Task<QueueMessage> GetMessage(CancellationToken cancellation)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            var holder = new HandlerHolder(cancellation);

            var compositeCancellation = CancellationTokenHelpers
                .Normalize(_disposedCancellation.Token, cancellation);

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

            return await holder.Completion.Task
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

                    onAck(_messageCancellation.Token);
                };

                Action<bool> nackHandler = requeue =>
                {
                    if (cancellation.IsCancellationRequested)
                        return;

                    onNack(_messageCancellation.Token, requeue);
                };

                var holder = new MessageHolder(new QueueMessage(msg, ackHandler, nackHandler),
                    cancellation.Token);

                _messageQueue.Enqueue(holder, cancellation.Token);
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

            var oldCancellation = _messageCancellation;
            _messageCancellation = new CancellationTokenSource();
            oldCancellation.Cancel();
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
            public HandlerHolder(CancellationToken cancellation)
            {
                cancellation.Register(() => { Completion.TrySetCanceled(); });
            }

            public TaskCompletionSource<QueueMessage> Completion { get; }
                = new TaskCompletionSource<QueueMessage>();
        }

        #endregion
    }
}