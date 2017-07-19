using System;
using System.Threading.Tasks;

namespace RabbitLink.Topology
{
    public interface ILinkProducerTopologyHandler
    {
        Task<ILinkExchage> Configure(ILinkTopologyConfig config);
        Task ConfigurationError(Exception ex);
    }
}