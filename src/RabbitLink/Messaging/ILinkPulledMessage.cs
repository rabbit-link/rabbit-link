#region Usings

using RabbitLink.Consumer;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Represents RabbitMQ message recieved from broker by <see cref="ILinkPullConsumer" />
    /// </summary>
    public interface ILinkPulledMessage : ILinkConsumedMessage
    {
        /// <summary>
        ///     ACK message
        /// </summary>
        void Ack();

        /// <summary>
        ///     NACK message
        /// </summary>
        /// <param name="requeue">If true returns message to queue</param>
        void Nack(bool requeue = false);
    }
}