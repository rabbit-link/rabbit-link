#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Consumer;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkConsumerMessageHandlerBuilder
    {
        public delegate LinkConsumerMessageHandlerDelegate<byte[]> HandlerFactory(
            ILinkSerializer serializer,
            LinkTypeNameMapping mapping
        );

        private LinkConsumerMessageHandlerBuilder(
            HandlerFactory factory,
            bool serializer,
            bool mapping
        )
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Serializer = serializer;
            Mapping = mapping;
        }

        public bool Mapping { get; }
        public bool Serializer { get; }
        public HandlerFactory Factory { get; }

        public static LinkConsumerMessageHandlerBuilder Create(
            LinkConsumerMessageHandlerDelegate<byte[]> onMessage
        )
            => new LinkConsumerMessageHandlerBuilder(
                (serializer, mapping) => onMessage,
                false,
                false
            );

        public static LinkConsumerMessageHandlerBuilder Create<TBody>(
            LinkConsumerMessageHandlerDelegate<TBody> onMessage
        ) where TBody : class
        {
            if (typeof(TBody) == typeof(byte[]) || typeof(TBody) == typeof(object))
                throw new ArgumentException("Type of TBody must be concrete and not equal byte[]");

            return new LinkConsumerMessageHandlerBuilder(
                (serializer, mapping) => msg =>
                {
                    TBody body;
                    var props = msg.Properties.Clone();

                    try
                    {
                        body = serializer.Deserialize<TBody>(msg.Body, props);
                    }
                    catch (Exception ex)
                    {
                        var sException = new LinkDeserializationException(msg, typeof(TBody), ex);
                        return Task.FromException<LinkConsumerAckStrategy>(sException);
                    }

                    var concreteMsg = new LinkConsumedMessage<TBody>(
                        body,
                        props,
                        msg.ReceiveProperties,
                        msg.Cancellation
                    );

                    return onMessage(concreteMsg);
                },
                true,
                false
            );
        }

        public static LinkConsumerMessageHandlerBuilder Create(
            LinkConsumerMessageHandlerDelegate<object> onMessage
        )
            => new LinkConsumerMessageHandlerBuilder(
                (serializer, mapping) => msg =>
                {
                    object body;
                    var props = msg.Properties.Clone();

                    var typeName = props.Type;

                    if (string.IsNullOrWhiteSpace(typeName))
                        return Task.FromException<LinkConsumerAckStrategy>(new LinkConsumerTypeNameMappingException());

                    typeName = typeName!.Trim();
                    var bodyType = mapping.Map(typeName);

                    if (bodyType == null)
                        return Task.FromException<LinkConsumerAckStrategy>(
                            new LinkConsumerTypeNameMappingException(typeName));

                    try
                    {
                        body = serializer.Deserialize(bodyType, msg.Body, props);
                    }
                    catch (Exception ex)
                    {
                        var sException = new LinkDeserializationException(msg, bodyType, ex);
                        return Task.FromException<LinkConsumerAckStrategy>(sException);
                    }

                    var concreteMsg = LinkMessageFactory
                        .ConstructConsumedMessage(bodyType, body, props, msg.ReceiveProperties, msg.Cancellation);

                    return onMessage(concreteMsg);
                },
                true,
                true
            );
    }
}
