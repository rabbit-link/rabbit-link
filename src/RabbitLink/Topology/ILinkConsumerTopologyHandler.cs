using System;
using System.Threading.Tasks;

namespace RabbitLink.Topology
{
    public interface ILinkConsumerTopologyHandler
    {
        Task<ILinkQueue> Configure(ILinkTopologyConfig config);
        Task ConfigurationError(Exception ex);
    }
}