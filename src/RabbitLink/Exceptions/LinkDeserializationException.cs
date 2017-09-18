using System;
using RabbitLink.Messaging;

namespace RabbitLink.Exceptions
{
    /// <summary>
    /// Fires when message cannot be deserialized
    /// </summary>
    public class LinkDeserializationException:LinkException
    {
        /// <summary>
        /// Constructs instance
        /// </summary>
        public LinkDeserializationException(ILinkConsumedMessage<byte[]> rawMessage, Type targetBodyType, Exception innerException)
            : base("Cannot deserialize message, see InnerException for details", innerException)
        {
            RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));
            TargetBodyType = targetBodyType ?? throw new ArgumentNullException(nameof(targetBodyType));
        }

        /// <summary>
        /// Raw message
        /// </summary>
        public ILinkConsumedMessage<byte[]> RawMessage { get; }
        
        /// <summary>
        /// Target body type
        /// </summary>
        public Type TargetBodyType { get; }
    }
}