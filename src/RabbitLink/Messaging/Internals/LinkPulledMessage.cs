#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Exceptions;

#endregion

namespace RabbitLink.Messaging.Internals
{
    internal class LinkPulledMessage<TBody> : LinkConsumedMessage<TBody>, ILinkPulledMessage<TBody> where TBody : class
    {
        private static LinkPulledMessageAckDelegate CreateAckDelegate(TaskCompletionSource<object> completion)
        {
            return () => completion.TrySetResult(null);
        }

        private static LinkPulledMessageNackDelegate CreateNackDelegate(TaskCompletionSource<object> completion)
        {
            return requeue => completion.TrySetException(new LinkConsumerNackException(
                requeue, "NACKed by pulled message"
            ));
        }

        #region Ctor

        public LinkPulledMessage(
            ILinkConsumedMessage<TBody> message,
            TaskCompletionSource<object> completion
        ) : base(
            message.Body,
            message.Properties,
            message.RecieveProperties,
            message.Cancellation
        )
        {
            if (completion == null)
                throw new ArgumentNullException(nameof(completion));

            Ack = CreateAckDelegate(completion);
            Nack = CreateNackDelegate(completion);
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
        }

        #endregion

        public LinkPulledMessageAckDelegate Ack { get; }
        public LinkPulledMessageNackDelegate Nack { get; }
    }
}