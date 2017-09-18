using System;
using System.Threading;

namespace RabbitLink.Messaging.Internals
{
    internal static class LinkMessageFactory
    {
        private static readonly Type LinkConsumedMessageType = typeof (LinkConsumedMessage<>);
        
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
    }
}