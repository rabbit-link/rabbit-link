#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    public class LinkMessage<T> : ILinkMessage<T> where T : class
    {
        public LinkMessage(T body, LinkMessageProperties properties)
        {
            if (body == null)
                throw new ArgumentNullException(nameof(body));

            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            Properties = properties;
            Body = body;
        }

        public LinkMessage(T body) : this(body, new LinkMessageProperties())
        {
        }

        public LinkMessageProperties Properties { get; }
        public T Body { get; }
    }
}