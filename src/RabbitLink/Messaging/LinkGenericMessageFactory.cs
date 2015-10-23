#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    internal static class LinkGenericMessageFactory
    {
        private static readonly Type LinkMessageType = typeof (LinkMessage<>);
        private static readonly Type LinkRecievedMessageType = typeof (LinkRecievedMessage<>);
        private static readonly Type LinkAckableRecievedMessageType = typeof (LinkAckableRecievedMessage<>);

        public static ILinkMessage<object> ConstructLinkMessage(Type bodyType, object body,
            LinkMessageProperties properties)
        {
            var genericType = LinkMessageType.MakeGenericType(bodyType);
            return (ILinkMessage<object>) Activator
                .CreateInstance(genericType, body, properties);
        }

        public static ILinkRecievedMessage<object> ConstructLinkRecievedMessage(
            Type bodyType,
            object body,
            LinkMessageProperties properties,
            LinkRecievedMessageProperties recievedProperties
            )
        {
            var genericType = LinkRecievedMessageType.MakeGenericType(bodyType);
            return (ILinkRecievedMessage<object>) Activator
                .CreateInstance(genericType, body, properties, recievedProperties);
        }

        public static ILinkRecievedMessage<object> ConstructLinkRecievedMessage(
            Type bodyType,
            ILinkMessage<object> message,
            LinkRecievedMessageProperties recievedProperties
            )
        {
            return ConstructLinkRecievedMessage(
                bodyType,
                message.Body,
                message.Properties,
                recievedProperties
                );
        }

        public static ILinkAckableRecievedMessage<object> ConstructLinkAckableRecievedMessage(
            Type bodyType,
            object body,
            LinkMessageProperties properties,
            LinkRecievedMessageProperties recievedProperties,
            Action ackAction,
            Action<bool> nackAction
            )
        {
            var genericType = LinkAckableRecievedMessageType.MakeGenericType(bodyType);
            return (ILinkAckableRecievedMessage<object>) Activator
                .CreateInstance(genericType, body, properties, recievedProperties, ackAction, nackAction);
        }

        public static ILinkAckableRecievedMessage<object> ConstructLinkAckableRecievedMessage(
            Type bodyType,
            ILinkMessage<object> message,
            LinkRecievedMessageProperties recievedProperties,
            Action ackAction,
            Action<bool> nackAction
            )
        {
            return ConstructLinkAckableRecievedMessage(
                bodyType,
                message.Body,
                message.Properties,
                recievedProperties,
                ackAction, nackAction
                );
        }

        public static ILinkAckableRecievedMessage<object> ConstructLinkAckableRecievedMessage(
            Type bodyType,
            ILinkRecievedMessage<object> message,
            Action ackAction,
            Action<bool> nackAction
            )
        {
            return ConstructLinkAckableRecievedMessage(bodyType, message, message.RecievedProperties, ackAction,
                nackAction);
        }
    }
}