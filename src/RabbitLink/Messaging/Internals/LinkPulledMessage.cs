#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Consumer;
using RabbitLink.Exceptions;

#endregion

namespace RabbitLink.Messaging.Internals
{
    internal class LinkPulledMessage<TBody> : LinkConsumedMessage<TBody>, ILinkPulledMessage<TBody> where TBody : class
    {
        private static LinkPulledMessageActionDelegate CreateActionDelegate(
            TaskCompletionSource<LinkConsumerAckStrategy> completion,
            LinkConsumerAckStrategy strategy
        )
        {
            return () => completion.TrySetResult(strategy);
        }

        private static LinkPulledMessageExceptionDelegate CreateExceptionDelegate(
            TaskCompletionSource<LinkConsumerAckStrategy> completion
        )
        {
            return ex => completion.TrySetException(ex);
        }

        #region Ctor

        public LinkPulledMessage(
            ILinkConsumedMessage<TBody> message,
            TaskCompletionSource<LinkConsumerAckStrategy> completion
        ) : base(
            message.Body,
            message.Properties,
            message.RecieveProperties,
            message.Cancellation
        )
        {
            if (completion == null)
                throw new ArgumentNullException(nameof(completion));

            Ack = CreateActionDelegate(completion, LinkConsumerAckStrategy.Ack);
            Nack = CreateActionDelegate(completion, LinkConsumerAckStrategy.Nack);
            Requeue = CreateActionDelegate(completion, LinkConsumerAckStrategy.Requeue);
            Exception = CreateExceptionDelegate(completion);
        }

        public LinkPulledMessage(
            LinkPulledMessage<byte[]> message,
            TBody body,
            LinkMessageProperties properties
        ) : base(
            body,
            properties,
            message.RecieveProperties,
            message.Cancellation
        )
        {
            Ack = message.Ack;
            Nack = message.Nack;
            Requeue = message.Requeue;
            Exception = message.Exception;
        }

        #endregion


        public LinkPulledMessageActionDelegate Ack { get; }
        public LinkPulledMessageActionDelegate Nack { get; }
        public LinkPulledMessageActionDelegate Requeue { get; }
        public LinkPulledMessageExceptionDelegate Exception { get; }
    }
}
