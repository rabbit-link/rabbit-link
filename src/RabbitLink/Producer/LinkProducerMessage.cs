#region Usings

using System;
using System.Threading;
using RabbitLink.Internals.Channels;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Producer
{
    internal class LinkProducerMessage : ChannelItem
    {
        public LinkProducerMessage(byte[] body, LinkMessageProperties properties,
            LinkPublishProperties publishProperties, CancellationToken cancellation) : base(cancellation)
        {
            Body = body;
            Properties = properties;
            PublishProperties = publishProperties;
        }

        public ReadOnlyMemory<byte> Body { get; }
        public LinkMessageProperties Properties { get; }
        public LinkPublishProperties PublishProperties { get; }
    }
}
