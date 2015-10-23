#region Usings

using System;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Exceptions
{
    public class LinkDeserializationException : LinkException
    {
        public LinkDeserializationException(ILinkRecievedMessage<byte[]> rawMessage, Exception innerException)
            : base("Cannot deserialize message, see InnerException for details", innerException)
        {
            RawMessage = rawMessage;
        }

        public ILinkRecievedMessage<byte[]> RawMessage { get; }
    }
}