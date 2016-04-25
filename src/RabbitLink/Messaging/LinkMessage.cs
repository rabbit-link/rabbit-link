#region Usings

using System;
using System.CodeDom;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using RabbitLink.Exceptions;

#endregion

namespace RabbitLink.Messaging
{
    internal class LinkMessage<T> : ILinkMessage<T> where T : class
    {
        private readonly CancellationToken _messageCancellation;
        private readonly LinkMessageOnAckAsyncDelegate _onAck;
        private readonly LinkMessageOnNackAsyncDelegate _onNack;
        private CancellationTokenSource _messageOperationCancellationSource;
        private readonly CancellationToken _messageOperationCancellation;
        private readonly object _sync = new object();

        public LinkMessage(T body, LinkMessageProperties properties, LinkRecieveMessageProperties recieveProperties,
            LinkMessageOnAckAsyncDelegate onAck, LinkMessageOnNackAsyncDelegate onNack,
            CancellationToken messageCancellation)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            if (recieveProperties == null)
                throw new ArgumentNullException(nameof(recieveProperties));

            Body = body;
            Properties = properties;
            RecieveProperties = recieveProperties;
            _onAck = onAck;
            _onNack = onNack;
            _messageCancellation = messageCancellation;

            _messageOperationCancellationSource = new CancellationTokenSource();
            _messageOperationCancellation = _messageOperationCancellationSource.Token;
        }

        public LinkMessage(T body, LinkMessage<byte[]> rawMessage)
            : this(
                body, rawMessage.Properties, rawMessage.RecieveProperties, rawMessage._onAck, rawMessage._onNack,
                rawMessage._messageCancellation)
        {
        }

        public LinkMessageProperties Properties { get; }
        public LinkRecieveMessageProperties RecieveProperties { get; }
        public T Body { get; }

        #region Factory

        public static ILinkMessage<object> Create(Type bodyType, object body, LinkMessage<byte[]> rawMessage)
        {
            var genericType = typeof(LinkMessage<>).MakeGenericType(bodyType);
            var ret =  Activator.CreateInstance(genericType, body, rawMessage);
            return ret as ILinkMessage<object>;
        }

        #endregion

        #region Private methods

        private void OnOperationSuccess()
        {
            if(_messageOperationCancellationSource == null)
                return;            

            lock (_sync)
            {
                if (_messageOperationCancellationSource == null)
                    return;

                _messageOperationCancellationSource.Cancel();
                _messageOperationCancellationSource.Dispose();
                _messageOperationCancellationSource = null;
            }
        }

        private async Task DoMessageOperationAsync(Func<Action, CancellationToken, Task> operation, CancellationToken? cancellation)
        {
            if (operation == null)
                return;

            if (cancellation == null)
            {
                cancellation = CancellationToken.None;
            }

            using (var compositeCancellation = CancellationTokenHelpers.Normalize(_messageCancellation, _messageOperationCancellation, cancellation.Value))
            {
                try
                {
                    compositeCancellation.Token.ThrowIfCancellationRequested();
                    await operation(OnOperationSuccess, compositeCancellation.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (_messageCancellation.IsCancellationRequested)
                    {
                        OnOperationSuccess();
                        throw new LinkMessageOperationException(
                            "Channel was closed before operation complete, message will be re-send");
                    }

                    if (_messageOperationCancellation.IsCancellationRequested)
                    {
                        throw new LinkMessageOperationException("Message already ACKed, NACked or Requeued");
                    }

                    throw;
                }
            }
        }        

        #endregion

        #region Public methods

        public virtual Task AckAsync(CancellationToken? cancellation)
        {
            Func<Action, CancellationToken, Task> operation = null;

            if (_onAck != null)
            {
                operation = (onSuccess, token) => _onAck(onSuccess, token);
            }

            return DoMessageOperationAsync(operation, cancellation);
        }

        public virtual Task Nack(CancellationToken? cancellation)
        {
            Func<Action, CancellationToken, Task> operation = null;

            if (_onNack != null)
            {
                operation = (onSuccess, token) => _onNack(false, onSuccess, token);
            }

            return DoMessageOperationAsync(operation, cancellation);            
        }

        public virtual Task Requeue(CancellationToken? cancellation)
        {
            Func<Action, CancellationToken, Task> operation = null;

            if (_onNack != null)
            {
                operation = (onSuccess, token) => _onNack(true, onSuccess, token);
            }

            return DoMessageOperationAsync(operation, cancellation);
        }

        #endregion               
    }
}