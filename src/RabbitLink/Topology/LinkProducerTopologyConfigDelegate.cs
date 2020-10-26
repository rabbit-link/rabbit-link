#region Usings

using System.Threading.Tasks;
using RabbitLink.Producer;

#endregion

namespace RabbitLink.Topology
{
    /// <summary>
    ///     Topology config delegate for <see cref="ILinkProducer" />
    /// </summary>
    public delegate Task<ILinkExchange> LinkProducerTopologyConfigDelegate(ILinkTopologyConfig config);
}
