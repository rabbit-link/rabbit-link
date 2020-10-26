namespace RabbitLink.Messaging
{
    /// <inheritdoc />
    /// <summary>
    ///     Represents RabbitMQ message received from broker by <see cref="T:RabbitLink.Consumer.ILinkPullConsumer" />
    /// </summary>
    public interface ILinkPulledMessage<out TBody> : ILinkConsumedMessage<TBody> where TBody : class
    {
        /// <summary>
        ///     ACK message delegate
        /// </summary>
        LinkPulledMessageActionDelegate Ack { get; }

        /// <summary>
        ///     NACK message delegate
        /// </summary>
        LinkPulledMessageActionDelegate Nack { get; }

        /// <summary>
        ///     Requeue message delegate
        /// </summary>
        LinkPulledMessageActionDelegate Requeue { get; }

        /// <summary>
        /// Pass exception to error strategy
        /// </summary>
        LinkPulledMessageExceptionDelegate Exception { get; }
    }
}
