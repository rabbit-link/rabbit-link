namespace RabbitLink.Exceptions
{
    /// <summary>
    /// Fired when published message was NACKed
    /// </summary>
    public class LinkMessageNackedException : LinkMessagePublishException
    {
        /// <summary>
        /// Constructs instance
        /// </summary>
        public LinkMessageNackedException() : base("Message NACKed")
        {
        }
    }
}