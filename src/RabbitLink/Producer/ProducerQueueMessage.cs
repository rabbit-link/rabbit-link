#region Usings

using System.Threading;
using RabbitLink.Internals;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Producer
{
    internal class LinkProducerQueueMessage : LinkAbstractQueueMessage
    {
        public LinkProducerQueueMessage(byte[] body, LinkMessageProperties properties,
            LinkPublishProperties publishProperties, CancellationToken cancellation) : base(cancellation)
        {
            Body = body;
            Properties = properties;
            PublishProperties = publishProperties;
        }

        public byte[] Body { get; }
        public LinkMessageProperties Properties { get; }
        public LinkPublishProperties PublishProperties { get; }
        public ulong Sequence { get; set; }
    }
}