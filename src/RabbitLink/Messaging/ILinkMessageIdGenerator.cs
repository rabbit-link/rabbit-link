namespace RabbitLink.Messaging
{
    /// <summary>
    /// MessageId generator
    /// </summary>
    public interface ILinkMessageIdStrategy
    {
        /// <summary>
        /// Generate MessageId
        /// </summary>
        /// <param name="message">Message to generate id</param>
        /// <returns>MessageId value</returns>
        void SetMessageId(ILinkMessage<byte[]> message);
    }
}