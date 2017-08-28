namespace RabbitLink.Messaging
{
    /// <summary>
    /// MessageId generator
    /// </summary>
    public interface ILinkMessageIdGenerator
    {
        /// <summary>
        /// Set message id
        /// </summary>                
        void SetMessageId(byte[] body, LinkMessageProperties properties, LinkPublishProperties publishProperties);
    }
}