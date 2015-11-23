#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    ///     MessageId generator which uses <see cref="Guid.NewGuid" />
    /// </summary>
    public class LinkGuidMessageIdStrategy : ILinkMessageIdStrategy
    {
        public void SetMessageId(ILinkMessage<byte[]> message)
        {
            message.Properties.MessageId = Guid.NewGuid().ToString("D");
        }
    }
}