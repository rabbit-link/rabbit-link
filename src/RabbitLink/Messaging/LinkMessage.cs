#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    /// Message
    /// </summary>
    public class LinkMessage
    {
        /// <summary>
        /// Makes instance
        /// </summary>
        /// <param name="body">Body value</param>
        /// <param name="properties">Message properties</param>
        public LinkMessage(
            byte[] body,
            LinkMessageProperties properties = null
        )
        {
            Body = body;
            Properties = properties ?? new LinkMessageProperties();
        }

        /// <summary>
        /// Message properties
        /// </summary>
        public LinkMessageProperties Properties { get; }
        
        /// <summary>
        /// Message body
        /// </summary>
        public byte[] Body { get; }
    }
}