#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using RabbitLink.Internals;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkConsumerMessageQueue : IDisposable
    {
        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly CancellationToken _disposedCancellation;

        private readonly LinkQueue<LinkQueueMessage<LinkMessage<byte[]>>> _handlersQueue =
            new LinkQueue<LinkQueueMessage<LinkMessage<byte[]>>>();

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

            // ReSharper disable once MethodSupportsCancellation
            _loopTask.WaitWithoutException();            

            _handlersQueue.Dispose();            
        }

        #region Loop

        private async Task LoopAsync()
        {
            while (!_disposedCancellation.IsCancellationRequested)
            {
                MessageHolder messageHolder;
                LinkQueueMessage<LinkMessage<byte[]>> handlerHolder;

                try
                {
                    messageHolder = await _messageQueue.DequeueAsync(_disposedCancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                using (var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, messageHolder.Cancellation))
                {                    
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
                }

                handlerHolder.SetResult(messageHolder.Message);
            }
        }

        #endregion

        public async Task<LinkMessage<byte[]>> GetMessageAsync(CancellationToken cancellation)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            var holder = new LinkQueueMessage<LinkMessage<byte[]>>(cancellation);

            try
            {
                await _handlersQueue.EnqueueAsync(holder)
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
                return await holder.Task
                    .ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Enqueue(byte[] body, LinkMessageProperties properties, LinkRecieveMessageProperties recieveProperties, LinkMessageOnAckAsyncDelegate onAck,
            LinkMessageOnNackAsyncDelegate onNack)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            var cancellation = _messageCancellation;

            try
            {
                var message = new LinkMessage<byte[]>(body, properties, recieveProperties, onAck, onNack, cancellation);
                var holder = new MessageHolder(message, cancellation);
                _messageQueue.Enqueue(holder, cancellation);
            }
            catch (InvalidOperationException)
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

        #endregion
    }
}