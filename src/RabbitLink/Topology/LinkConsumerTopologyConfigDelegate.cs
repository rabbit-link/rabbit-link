using System.Threading.Tasks;
using RabbitLink.Consumer;

namespace RabbitLink.Topology
{
    /// <summary>
    /// Topology config delegate for <see cref="ILinkConsumer"/>
    /// </summary>
    public delegate Task<ILinkQueue> LinkConsumerTopologyConfigDelegate(ILinkTopologyConfig config);
}
