namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Represents RabbitMQ message base
    /// </summary>
    public abstract class LinkMessage : ILinkMessage
    {
        #region Ctor

        /// <summary>
        ///     Makes instance
        /// </summary>
        /// <param name="body">Body value</param>
        /// <param name="properties">Message properties</param>
        protected LinkMessage(
            byte[] body,
            LinkMessageProperties properties = null
        )
        {
            Body = body;
            Properties = properties ?? new LinkMessageProperties();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Message properties
        /// </summary>
        public LinkMessageProperties Properties { get; }

        /// <summary>
        ///     Message body
        /// </summary>
        public byte[] Body { get; }

        #endregion
    }
}