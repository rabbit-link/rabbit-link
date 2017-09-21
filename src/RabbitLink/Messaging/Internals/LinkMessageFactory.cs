using System;
using System.Threading;

namespace RabbitLink.Messaging.Internals
{
    internal static class LinkMessageFactory
    {
        private static readonly Type LinkConsumedMessageType = typeof(LinkConsumedMessage<>);
        private static readonly Type LinkPulledMessageType = typeof(LinkPulledMessage<>);

        public static ILinkConsumedMessage<object> ConstructConsumedMessage(
            Type bodyType,
            object body,
            LinkMessageProperties properties,
            LinkRecieveProperties recievedProperties,
            CancellationToken cancellation
        )
        {
            var genericType = LinkConsumedMessageType.MakeGenericType(bodyType);
            return (ILinkConsumedMessage<object>) Activator
                .CreateInstance(genericType, body, properties, recievedProperties, cancellation);
        }

        public static ILinkPulledMessage<object> ConstructPulledMessage(
            Type bodyType,
            LinkPulledMessage<byte[]> message,
            object body,
            LinkMessageProperties properties
        )
        {
            var genericType = LinkPulledMessageType.MakeGenericType(bodyType);
            return (ILinkPulledMessage<object>) Activator
                .CreateInstance(genericType, message, body, properties);
        }
    }
}
