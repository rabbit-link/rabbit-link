namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Represents RabbitMQ message for publish
    /// </summary>
    public interface ILinkPublishMessage : ILinkMessage
    {
        #region Properties

        /// <summary>
        ///     Publish properties
        /// </summary>
        LinkPublishProperties PublishProperties { get; }

        #endregion
    }
}