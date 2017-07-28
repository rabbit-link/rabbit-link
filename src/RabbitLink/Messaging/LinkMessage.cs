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
            LinkMessageProperties properties
        )
        {
            Body = body;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }
        
        /// <summary>
        /// Makes instance with defailt Properties
        /// </summary>
        /// <param name="body">Body value</param>
        public LinkMessage(
            byte[] body
        ) : this(body, new LinkMessageProperties())
        {
            Body = body;
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