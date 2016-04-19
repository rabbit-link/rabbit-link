#region Usings

using System;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Exceptions
{
    public class LinkDeserializationException : LinkException
    {
        public LinkDeserializationException(byte[] body, LinkMessageProperties properties, LinkRecieveMessageProperties recieveProperties, Exception innerException)
            : base("Cannot deserialize message, see InnerException for details", innerException)
        {
            Body = body;
            Properties = properties;
            RecieveProperties = recieveProperties;
        }

        public byte[] Body { get; }

        public LinkMessageProperties Properties { get; }        

        public LinkRecieveMessageProperties RecieveProperties { get; }
    }
}