namespace RabbitLink.Messaging
{
    /// <summary>
    /// Represents RabbitMQ message
    /// </summary>
    public interface ILinkMessage<out TBody> where TBody : class
    {
        /// <summary>
        ///     Message properties
        /// </summary>
        LinkMessageProperties Properties { get; }

        /// <summary>
        ///     Message body
        /// </summary>
        TBody Body { get; }
    }
}