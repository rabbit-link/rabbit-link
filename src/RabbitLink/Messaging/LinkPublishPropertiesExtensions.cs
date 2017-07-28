using System;
using System.Linq;

namespace RabbitLink.Messaging
{
    /// <summary>
    /// Extension methods for <see cref="LinkPublishProperties"/>
    /// </summary>
    public static class LinkPublishPropertiesExtensions
    {
        /// <summary>
        /// Makes clone of instance
        /// </summary>
        public static LinkPublishProperties Clone(this LinkPublishProperties @this)
            => new LinkPublishProperties().Extend(@this);

        /// <summary>
        /// Extends instance with others. Returns new instance.
        /// </summary>
        public static LinkPublishProperties Extend(this LinkPublishProperties @this, params LinkPublishProperties[] others)
        {
            if(@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (others?.Any() == true)
            {
                foreach (var other in others)
                {
                    if (other.RoutingKey != null) @this.RoutingKey = other.RoutingKey;
                    if (other.Mandatory != null) @this.Mandatory = other.Mandatory;
                }
            }

            return @this;
        }
    }
}