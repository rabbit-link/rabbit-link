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
        public delegate LinkConsumerMessageHandlerDelegate<ReadOnlyMemory<byte>> HandlerFactory(
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
            LinkConsumerMessageHandlerDelegate<ReadOnlyMemory<byte>> onMessage
        )
            => new(
                (_, _) => onMessage,
                false,
                false
            );

        public static LinkConsumerMessageHandlerBuilder Create<TBody>(
            LinkConsumerMessageHandlerDelegate<TBody> onMessage
        ) where TBody : class
        {
            if (typeof(TBody) == typeof(ReadOnlyMemory<byte>) || typeof(TBody) == typeof(object))
                throw new ArgumentException("Type of TBody must be set and not equal ReadOnlyMemory<byte>");

            return new LinkConsumerMessageHandlerBuilder(
                (serializer, _) => msg =>
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

                    var typedMsg = new LinkConsumedMessage<TBody>(
                        body,
                        props,
                        msg.ReceiveProperties,
                        msg.Cancellation
                    );

                    return onMessage(typedMsg);
                },
                true,
                false
            );
        }

        public static LinkConsumerMessageHandlerBuilder Create(
            LinkConsumerMessageHandlerDelegate<object> onMessage
        )
            => new(
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
                            new LinkConsumerTypeNameMappingException(typeName)
                            );

                    try
                    {
                        body = serializer.Deserialize(bodyType, msg.Body, props);
                    }
                    catch (Exception ex)
                    {
                        var sException = new LinkDeserializationException(msg, bodyType, ex);
                        return Task.FromException<LinkConsumerAckStrategy>(sException);
                    }

                    var typedMsg = LinkMessageFactory
                        .ConstructConsumedMessage(bodyType, body, props, msg.ReceiveProperties, msg.Cancellation);

                    return onMessage(typedMsg);
                },
                true,
                true
            );
    }
}
