namespace RabbitLink.Consumer
{
    public enum LinkConsumerAckStrategy
    {
        /// <summary>
        ///     Ack message
        /// </summary>
        Ack,

        /// <summary>
        ///     Nack message.
        ///     Message will be removed from the queue.
        ///     If queue has dead-letter queue, message will be added to it.
        /// </summary>
        Nack,

        /// <summary>
        ///     Return message to queue.
        ///     This or another consumer will process it later.
        /// </summary>
        NackWithRequeue
    }
}