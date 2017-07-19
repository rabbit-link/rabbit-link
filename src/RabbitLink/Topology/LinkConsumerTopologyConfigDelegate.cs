using System.Threading.Tasks;

namespace RabbitLink.Topology
{
    public delegate Task<ILinkQueue> LinkConsumerTopologyConfigDelegate(ILinkTopologyConfig config);
}