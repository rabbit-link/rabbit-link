using System;

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
        void SetMessageId(ReadOnlyMemory<byte> body, LinkMessageProperties properties, LinkPublishProperties publishProperties);
    }
}
