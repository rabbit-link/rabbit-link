namespace RabbitLink.Exceptions
{
    /// <summary>
    /// Fired when published message was Returned
    /// </summary>
    public class LinkMessageReturnedException : LinkMessagePublishException
    {
        /// <summary>
        /// Constructs instance with reason
        /// </summary>
        /// <param name="reason"></param>
        public LinkMessageReturnedException(string reason) : base($"Message Returned: {reason}")
        {
        }
    }
}
