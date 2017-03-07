#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    public class LinkPublishProperties 
    {
        public string RoutingKey { get; set; }
        public bool? Mandatory { get; set; }
        
        public LinkPublishProperties Clone()
        {
            return (LinkPublishProperties) MemberwiseClone();
        }

        public virtual void Extend(LinkPublishProperties properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            if (properties.Mandatory != null) Mandatory = properties.Mandatory;
            if (properties.RoutingKey != null) RoutingKey = properties.RoutingKey;
        }
    }
}