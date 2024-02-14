namespace RabbitLink.Messaging
{
    /// <inheritdoc cref="ILinkPublishMessage{TBody}" />
    public class LinkPublishMessage<TBody> : LinkMessage<TBody>, ILinkPublishMessage<TBody>
    {
        #region Ctor

        /// <summary>
        ///     Creates new instance
        /// </summary>
        /// <param name="body">Body value</param>
        /// <param name="properties">Message properties</param>
        /// <param name="publishProperties">Publish properties</param>
        public LinkPublishMessage(
            TBody body,
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
        ///     Creates new instance from another message
        /// </summary>
        /// <param name="message">Message instance</param>
        /// <param name="publishProperties">Publish properties</param>
        public LinkPublishMessage(
            ILinkMessage<TBody> message,
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

        /// <inheritdoc />
        public LinkPublishProperties PublishProperties { get; }

        #endregion
    }
}
