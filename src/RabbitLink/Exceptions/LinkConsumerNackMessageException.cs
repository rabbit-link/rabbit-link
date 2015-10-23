namespace RabbitLink.Exceptions
{
    /// <summary>
    ///     Informs consumer than message must be NACKed
    /// </summary>
    public class LinkConsumerNackMessageException : LinkException
    {
        public LinkConsumerNackMessageException(bool requeue = false) : base("Nack message from queue")
        {
            Requeue = requeue;
        }

        /// <summary>
        ///     Is message must be returned to queue
        /// </summary>
        public bool Requeue { get; }
    }
}