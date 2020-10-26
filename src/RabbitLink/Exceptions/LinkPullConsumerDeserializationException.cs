#region Usings

using System;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Exceptions
{
    /// <summary>
    ///     Fires when message cannot be deserialized by pull consumer
    /// </summary>
    public class LinkPullConsumerDeserializationException : LinkException
    {
        /// <summary>
        ///     Constructs instance
        /// </summary>
        public LinkPullConsumerDeserializationException(
            ILinkPulledMessage<byte[]> rawMessage, Type targetBodyType, Exception innerException)
            : base("Cannot deserialize message, see InnerException for details", innerException)
        {
            RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));
            TargetBodyType = targetBodyType;
        }

        /// <summary>
        ///     Raw message
        /// </summary>
        public ILinkPulledMessage<byte[]> RawMessage { get; }

        /// <summary>
        ///     Target body type
        /// </summary>
        public Type TargetBodyType { get; }
    }
}
