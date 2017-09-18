using System;
using System.Threading.Tasks;
using RabbitLink.Consumer;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;
using RabbitLink.Serialization;

namespace RabbitLink.Builders
{
    internal class LinkConsumerMessageHandlerBuilder
    {
        private static LinkConsumerMessageHandlerDelegate<byte[]> GetDeserializingHandler(
            Type type,
            Func<ILinkConsumedMessage<object>, Task> onMessage
        )
        {
            return (message, serializer) =>
            {
                object body;
                var props = message.Properties.Clone();

                try
                {
                    body = serializer.Deserialize(type, message.Body, props);
                }
                catch (Exception ex)
                {
                    throw new LinkDeserializationException(message, type, ex);
                }

                var concreteMessage = LinkMessageFactory.ConstructConsumedMessage(
                    type,
                    body,
                    props,
                    message.RecieveProperties,
                    message.Cancellation
                );

                return onMessage(concreteMessage);
            };
        }

        
        public LinkConsumerMessageHandlerBuilder()
        {
            
        }

        public LinkConsumerMessageHandlerDelegate<byte[]> Build(
            ILinkSerializer serializer, 
            LinkTypeNameMapping mapping
        )
        {
            
        }
        
        
    }
}