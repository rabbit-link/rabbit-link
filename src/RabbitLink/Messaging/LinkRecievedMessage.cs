#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    public class LinkRecievedMessage<T> : LinkMessage<T>, ILinkRecievedMessage<T> where T : class
    {
        public LinkRecievedMessage(ILinkMessage<T> message, LinkRecievedMessageProperties recievedProperties)
            : this(message.Body, message.Properties, recievedProperties)
        {
        }

        public LinkRecievedMessage(T body, LinkMessageProperties properties,
            LinkRecievedMessageProperties recievedProperties)
            : base(body, properties)
        {
            if (recievedProperties == null)
                throw new ArgumentNullException(nameof(recievedProperties));

            RecievedProperties = recievedProperties;
        }

        public LinkRecievedMessageProperties RecievedProperties { get; }
    }
}