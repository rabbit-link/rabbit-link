namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Represents RabbitMQ message for publish
    /// </summary>
    public class LinkPublishMessage : LinkMessage, ILinkPublishMessage
    {
        #region Ctor

        /// <summary>
        ///     Creates new instance
        /// </summary>
        /// <param name="body">Body value</param>
        /// <param name="properties">Message properties</param>
        /// <param name="publishProperties">Publish properties</param>
        public LinkPublishMessage(
            byte[] body,
            LinkMessageProperties properties = null,
            LinkPublishProperties publishProperties = null
        ) : base(
            body,
            properties
        )
        {
            PublishProperties = publishProperties ?? new LinkPublishProperties();
        }

        /// <summary>
        ///     Creates new instance from <see cref="ILinkMessage" />
        /// </summary>
        /// <param name="message">Message instance</param>
        /// <param name="publishProperties">Publish properties</param>
        public LinkPublishMessage(
            ILinkMessage message,
            LinkPublishProperties publishProperties = null
        ) : this(
            message.Body,
            message.Properties,
            publishProperties
        )
        {
        }

        #endregion

        #region ILinkPublishMessage Members

        /// <summary>
        ///     Publish properties
        /// </summary>
        public LinkPublishProperties PublishProperties { get; }

        #endregion
    }
}