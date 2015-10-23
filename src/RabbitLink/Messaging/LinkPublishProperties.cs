#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    public class LinkPublishProperties : ICloneable
    {
        public string RoutingKey { get; set; }
        public bool? Mandatory { get; set; }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public LinkPublishProperties Clone()
        {
            return new LinkPublishProperties
            {
                RoutingKey = RoutingKey,
                Mandatory = Mandatory
            };
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