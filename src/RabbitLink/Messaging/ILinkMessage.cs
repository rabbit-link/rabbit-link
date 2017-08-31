namespace RabbitLink.Messaging
{
    /// <summary>
    /// Represents RabbitMQ message
    /// </summary>
    public interface ILinkMessage
    {
        /// <summary>
        ///     Message properties
        /// </summary>
        LinkMessageProperties Properties { get; }

        /// <summary>
        ///     Message body
        /// </summary>
        byte[] Body { get; }
    }
}