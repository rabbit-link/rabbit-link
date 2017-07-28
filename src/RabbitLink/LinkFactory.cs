using RabbitLink.Builders;

namespace RabbitLink
{
    /// <summary>
    /// Builder for <see cref="ILink"/>
    /// </summary>
    public sealed class LinkFactory
    {
        /// <summary>
        /// Returns new Builder
        /// </summary>
        public static ILinkBuilder Builder 
            => new LinkBuilder();
        
    }
}