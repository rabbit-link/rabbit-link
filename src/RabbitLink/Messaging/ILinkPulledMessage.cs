#region Usings

using RabbitLink.Consumer;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Represents RabbitMQ message recieved from broker by <see cref="ILinkPullConsumer" />
    /// </summary>
    public interface ILinkPulledMessage<out TBody> : ILinkConsumedMessage<TBody> where TBody: class
    {
        /// <summary>
        ///     ACK message delegate
        /// </summary>
        LinkPulledMessageAckDelegate Ack { get; }

        /// <summary>
        ///     NACK message delegate
        /// </summary>
        LinkPulledMessageNackDelegate Nack { get; }
    }
}