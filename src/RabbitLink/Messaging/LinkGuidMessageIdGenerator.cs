#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    ///     MessageId generator which uses <see cref="Guid.NewGuid" />
    /// </summary>
    public class LinkGuidMessageIdGenerator : ILinkMessageIdGenerator
    {
        /// <inheritdoc />
        public void SetMessageId(ReadOnlyMemory<byte> body, LinkMessageProperties properties, LinkPublishProperties publishProperties)
        {
            properties.MessageId = Guid.NewGuid().ToString("D");
        }
    }
}
