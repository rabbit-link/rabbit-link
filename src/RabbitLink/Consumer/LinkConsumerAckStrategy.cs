namespace RabbitLink.Consumer
{
    /// <summary>
    /// Consumer ACK strategies
    /// </summary>
    public enum LinkConsumerAckStrategy
    {
        /// <summary>
        /// ACK message
        /// </summary>
        Ack,
        /// <summary>
        /// NACK message
        /// </summary>
        Nack,
        /// <summary>
        /// Requeue message
        /// </summary>
        Requeue
    }
}