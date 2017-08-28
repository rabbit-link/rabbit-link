using System;

namespace RabbitLink.Exceptions
{
    /// <summary>
    ///     Informs consumer than message must be NACKed
    /// </summary>
    public class LinkConsumerNackException : LinkMessageConsumeException
    {
        /// <summary>
        /// Constructs intance
        /// </summary>
        /// <param name="requeue">Is message must be requeued</param>
        /// <param name="reason">Additional reason</param>
        public LinkConsumerNackException(bool requeue = false, string reason = "None") 
            : base($"Message NACKed by handler, requeue: {requeue}, reason: {reason}")
        {
            Requeue = requeue;
            Reason = reason;
        }

        /// <summary>
        /// Constructs intance
        /// </summary>
        /// <param name="innerException">inner exception</param>
        /// <param name="requeue">Is message must be requeued</param>
        /// <param name="reason">Additional reason</param>
        public LinkConsumerNackException(Exception innerException, bool requeue = false, string reason = "None")
            : base($"Message NACKed by handler, requeue: {requeue}, reason: {reason}", innerException)
        {
            Requeue = requeue;
            Reason = reason;
        }

        /// <summary>
        ///     Is message must be returned to queue
        /// </summary>
        public bool Requeue { get; }

        /// <summary>
        /// Additional reason
        /// </summary>
        public string Reason { get; }
    }
}