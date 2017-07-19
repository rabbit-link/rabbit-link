#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Exceptions;

#endregion

namespace RabbitLink.Messaging
{
    internal class LinkMessage<T> where T : class
    {
        public LinkMessage(
            T body,
            LinkMessageProperties properties
        )
        {
            Body = body;
            Properties = properties;
        }

        public LinkMessage(
            T body,
            LinkMessage<byte[]> rawMessage
        )
        : this(
            body,
            rawMessage.Properties
        )
        {
        }


        public LinkMessageProperties Properties { get; }
        public T Body { get; }
    }


    internal class LinkPushMessage<T> : LinkMessage<T>, ILinkPushMessage<T> where T : class
    {
        public LinkPushMessage(
            T body,
            LinkMessageProperties properties,
            LinkRecieveMessageProperties recieveProperties
        )
        : base(
            body,
            properties
        )
        {
            RecieveProperties = recieveProperties ?? throw new ArgumentNullException(nameof(recieveProperties));
        }

        public LinkPushMessage(
            T body,
            LinkPushMessage<byte[]> rawMessage
        ) : this(
            body,
            rawMessage.Properties,
            rawMessage.RecieveProperties
        )
        {
            
        }

        public LinkPushMessage(
            LinkMessage<T> msg,
            LinkRecieveMessageProperties recieveProperties
        ) : this(
            msg.Body,
            msg.Properties,
            recieveProperties
        )
        {

        }

        public LinkRecieveMessageProperties RecieveProperties { get; }

        public static LinkPushMessage<object> Create(Type bodyType, object body, LinkPushMessage<byte[]> rawMessage)
        {
            var genericType = typeof(LinkPushMessage<>).MakeGenericType(bodyType);
            var ret = Activator.CreateInstance(genericType, body, rawMessage);
            return ret as LinkPushMessage<object>;
        }
    }

    internal class LinkConsumedMessage<T> : LinkPushMessage<T>, ILinkMessage<T> where T : class
    {
        private readonly CancellationToken _messageCancellation;
        private readonly LinkMessageOnAckAsyncDelegate _onAck;
        private readonly LinkMessageOnNackAsyncDelegate _onNack;
        private CancellationTokenSource _messageOperationCancellationSource;
        private readonly CancellationToken _messageOperationCancellation;
        private readonly object _sync = new object();

        public LinkConsumedMessage(
            T body,
            LinkMessageProperties properties,
            LinkRecieveMessageProperties recieveProperties,
            LinkMessageOnAckAsyncDelegate onAck,
            LinkMessageOnNackAsyncDelegate onNack,
            CancellationToken messageCancellation
        ) : base(
            body,
            properties,
            recieveProperties
        )
        {

            _onAck = onAck;
            _onNack = onNack;
            _messageCancellation = messageCancellation;

            _messageOperationCancellationSource = new CancellationTokenSource();
            _messageOperationCancellation = _messageOperationCancellationSource.Token;
        }

        public LinkConsumedMessage(T body, LinkConsumedMessage<byte[]> rawMessage)
            : this(
                body,
                rawMessage.Properties,
                rawMessage.RecieveProperties,
                rawMessage._onAck,
                rawMessage._onNack,
                rawMessage._messageCancellation
        )
        {
        }

        public LinkMessage(
            LinkPushMessage<T> msg,
            LinkMessageOnAckAsyncDelegate onAck,
            LinkMessageOnNackAsyncDelegate onNack,
            CancellationToken messageCancellation
        ) : this(
            msg.Body,
            msg.Properties,
            msg.RecieveProperties,
            onAck,
            onNack,
            messageCancellation
        )
        {

        }



        #region Factory

        public new static LinkMessage<object> Create(Type bodyType, object body, LinkMessage<byte[]> rawMessage)
        {
            var genericType = typeof(LinkMessage<>).MakeGenericType(bodyType);
            var ret = Activator.CreateInstance(genericType, body, rawMessage);
            return ret as LinkMessage<object>;
        }

        #endregion

        #region Private methods

        private void OnOperationSuccess()
        {
            if (_messageOperationCancellationSource == null)
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

            using (var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(_messageCancellation, _messageOperationCancellation, cancellation.Value))
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

        public virtual Task NackAsync(CancellationToken? cancellation)
        {
            Func<Action, CancellationToken, Task> operation = null;

            if (_onNack != null)
            {
                operation = (onSuccess, token) => _onNack(false, onSuccess, token);
            }

            return DoMessageOperationAsync(operation, cancellation);
        }

        public virtual Task RequeueAsync(CancellationToken? cancellation)
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