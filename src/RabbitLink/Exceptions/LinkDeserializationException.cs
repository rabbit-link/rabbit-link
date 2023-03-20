#region Usings

using System;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Exceptions
{
    /// <summary>
    ///     Fires when message cannot be deserialized
    /// </summary>
    public class LinkDeserializationException : LinkException
    {
        /// <summary>
        ///     Constructs instance
        /// </summary>
        public LinkDeserializationException(ILinkConsumedMessage<ReadOnlyMemory<byte>> rawMessage, Type targetBodyType,
            Exception innerException)
            : base("Cannot deserialize message, see InnerException for details", innerException)
        {
            RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));
            TargetBodyType = targetBodyType;
        }

        /// <summary>
        ///     Raw message
        /// </summary>
        public ILinkConsumedMessage<ReadOnlyMemory<byte>> RawMessage { get; }

        /// <summary>
        ///     Target body type
        /// </summary>
        public Type TargetBodyType { get; }
    }
}
