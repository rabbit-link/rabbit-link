using System.Threading.Tasks;

namespace RabbitLink.Topology
{
    public delegate Task<ILinkExchage> LinkProducerTopologyConfigDelegate(ILinkTopologyConfig config);
}