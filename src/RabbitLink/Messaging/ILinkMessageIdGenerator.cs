namespace RabbitLink.Messaging
{
    /// <summary>
    /// MessageId generator
    /// </summary>
    public interface ILinkMessageIdGenerator
    {
        /// <summary>
        /// Generate MessageId
        /// </summary>                
        void SetMessageId(byte[] body, LinkMessageProperties properties, LinkPublishProperties publishProperties);
    }
}