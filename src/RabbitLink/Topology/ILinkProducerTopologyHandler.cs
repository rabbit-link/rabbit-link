using System;
using System.Threading.Tasks;
using RabbitLink.Producer;

namespace RabbitLink.Topology
{
    /// <summary>
    /// Topology handler for <see cref="ILinkProducer"/>
    /// </summary>
    public interface ILinkProducerTopologyHandler
    {
        /// <summary>
        /// Configure topology handler
        /// </summary>
        Task<ILinkExchage> Configure(ILinkTopologyConfig config);

        /// <summary>
        /// Topology configuration error handler
        /// </summary>
        Task ConfigurationError(Exception ex);
    }
}
