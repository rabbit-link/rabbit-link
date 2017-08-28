﻿#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    ///     MessageId generator which uses <see cref="Guid.NewGuid" />
    /// </summary>
    public class LinkGuidMessageIdGenerator : ILinkMessageIdGenerator
    {
        /// <summary>
        /// Set message id
        /// </summary>
        public void SetMessageId(byte[] body, LinkMessageProperties properties, LinkPublishProperties publishProperties)
        {
            properties.MessageId = Guid.NewGuid().ToString("D");
        }
    }
}