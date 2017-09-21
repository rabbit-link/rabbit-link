using System.Threading.Tasks;
using RabbitLink.Producer;

namespace RabbitLink.Topology
{
    /// <summary>
    /// Topology config delegate for <see cref="ILinkProducer"/>
    /// </summary>
    public delegate Task<ILinkExchage> LinkProducerTopologyConfigDelegate(ILinkTopologyConfig config);
}
