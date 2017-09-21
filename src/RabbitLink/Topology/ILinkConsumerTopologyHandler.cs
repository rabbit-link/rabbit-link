using System;
using System.Threading.Tasks;
using RabbitLink.Consumer;

namespace RabbitLink.Topology
{
    /// <summary>
    /// Topology handler for <see cref="ILinkConsumer"/>
    /// </summary>
    public interface ILinkConsumerTopologyHandler
    {
        /// <summary>
        /// Configure topology handler
        /// </summary>
        Task<ILinkQueue> Configure(ILinkTopologyConfig config);

        /// <summary>
        /// Topology configuration error handler
        /// </summary>
        Task ConfigurationError(Exception ex);
    }
}
