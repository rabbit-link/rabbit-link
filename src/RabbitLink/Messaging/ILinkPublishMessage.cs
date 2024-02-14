namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Represents RabbitMQ message for publish
    /// </summary>
    public interface ILinkPublishMessage<out TBody> : ILinkMessage<TBody>
    {
        #region Properties

        /// <summary>
        ///     Publish properties
        /// </summary>
        LinkPublishProperties PublishProperties { get; }

        #endregion
    }
}
