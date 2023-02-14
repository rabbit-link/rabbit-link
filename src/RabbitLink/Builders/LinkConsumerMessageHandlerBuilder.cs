#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            LinkTypeNameMapping mapping,
            IReadOnlyCollection<DeliveryInterceptDelegate> interceptors
        );


        private LinkConsumerMessageHandlerBuilder(
            HandlerFactory factory,
            IReadOnlyCollection<DeliveryInterceptDelegate> interceptors,
            bool serializer,
            bool mapping
        )
        {
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Interceptors = interceptors;
            Serializer = serializer;
            Mapping = mapping;
        }

        private IReadOnlyCollection<DeliveryInterceptDelegate> _interceptors = new List<DeliveryInterceptDelegate>();

        public bool Mapping { get; }
        public bool Serializer { get; }
        public HandlerFactory Factory { get; }
        public IReadOnlyCollection<DeliveryInterceptDelegate> Interceptors { get; }

        public static LinkConsumerMessageHandlerBuilder Create(
            LinkConsumerMessageHandlerDelegate<byte[]> onMessage
        )
            => new(
                (_, _, _) => onMessage,
                Array.Empty<DeliveryInterceptDelegate>(),
                false,
                false
            );

        public static LinkConsumerMessageHandlerBuilder Create<TBody>(
            LinkConsumerMessageHandlerDelegate<TBody> onMessage
        ) where TBody : class
        {
            if (typeof(TBody) == typeof(byte[]) || typeof(TBody) == typeof(object))
                throw new ArgumentException("Type of TBody must be set and not equal byte[]");

            return new LinkConsumerMessageHandlerBuilder(
                (serializer, _, interceptors) => msg =>
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
                    ILinkConsumedMessage<TBody> typedMsg = new LinkConsumedMessage<TBody>(
                        body,
                        props,
                        msg.ReceiveProperties,
                        msg.Cancellation
                    );

                    return onMessage(typedMsg);
                },
                Array.Empty<DeliveryInterceptDelegate>(),
                true,
                false
            );
        }

        public static LinkConsumerMessageHandlerBuilder Create(
            LinkConsumerMessageHandlerDelegate<object> onMessage
        )
            => new(
                (serializer, mapping, interceptors) => msg =>
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
                Array.Empty<DeliveryInterceptDelegate>(),
                true,
                true
            );
    }
}
