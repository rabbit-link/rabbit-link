using System;

namespace RabbitLink.Messaging
{
    /// <summary>
    /// Message for publish
    /// </summary>
    public class LinkPublishMessage: LinkMessage
    {
        /// <summary>
        /// Creates new instance
        /// </summary>
        /// <param name="body">Body value</param>
        /// <param name="properties">Message properties</param>
        /// <param name="publishProperties">Publish properties</param>
        public LinkPublishMessage(
            byte[] body, 
            LinkMessageProperties properties = null, 
            LinkPublishProperties publishProperties = null
        ) : base(
            body, 
            properties
        )
        {
            PublishProperties = publishProperties ?? new LinkPublishProperties();
        }
        
        /// <summary>
        /// Publish properties
        /// </summary>
        public LinkPublishProperties PublishProperties { get; }
    }
}