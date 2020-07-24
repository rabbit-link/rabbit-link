#region Usings

using System;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Exceptions
{
    /// <summary>
    ///     Fires when type name mapping not found in pull consumer
    /// </summary>
    public class LinkPullConsumerTypeNameMappingException : LinkException
    {
        /// <summary>
        ///     Constructs instance when no Type header in message
        /// </summary>
        public LinkPullConsumerTypeNameMappingException(ILinkPulledMessage<byte[]> rawMessage)
            : base("Message not contains Type header")
        {
            RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));
        }

        /// <summary>
        ///     Constructs instance when Type for Name not found
        /// </summary>
        public LinkPullConsumerTypeNameMappingException(ILinkPulledMessage<byte[]> rawMessage, string name)
            : base($"Cannot get mapping for TypeName {name}")
        {
            Name = name;
            RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));
        }

        /// <summary>
        ///     Mapping name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Raw message
        /// </summary>
        public ILinkPulledMessage<byte[]> RawMessage { get; }
    }
}
