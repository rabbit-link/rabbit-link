#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

#endregion

namespace RabbitLink.Messaging
{
    internal class LinkMessage<T> : ILinkMessage<T> where T : class
    {
        private readonly LinkMessageOnAckAsyncDelegate _onAck;
        private readonly LinkMessageOnNackAsyncDelegate _onNack;

        public LinkMessage(T body, LinkMessageProperties properties, LinkRecieveMessageProperties recieveProperties, LinkMessageOnAckAsyncDelegate onAck, LinkMessageOnNackAsyncDelegate onNack)
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
        }

        public LinkMessage(T body, LinkMessage<byte[]> rawMessage)
            :this(body, rawMessage.Properties, rawMessage.RecieveProperties, rawMessage._onAck, rawMessage._onNack)
        {                       
        }

        public LinkMessageProperties Properties { get; }
        public LinkRecieveMessageProperties RecieveProperties { get; }
        public T Body { get; }
        public virtual Task AckAsync(CancellationToken? cancellation)
        {
            return _onAck?.Invoke(cancellation ?? CancellationToken.None) ?? TaskConstants.Completed;
        }

        public virtual Task Nack(CancellationToken? cancellation)
        {
            return _onNack?.Invoke(false, cancellation ?? CancellationToken.None) ?? TaskConstants.Completed;
        }

        public virtual Task Requeue(CancellationToken? cancellation)
        {
            return _onNack?.Invoke(true, cancellation ?? CancellationToken.None) ?? TaskConstants.Completed;
        }

        public static LinkMessage<object> Create(Type bodyType, object body, LinkMessage<byte[]> rawMessage)
        {
            var genericType = typeof(LinkMessage<>).MakeGenericType(bodyType);
            return (LinkMessage<object>)Activator.CreateInstance(genericType, body, rawMessage);
        }
    }
}