#region Usings

using System.Threading.Tasks;
using RabbitLink.Consumer;

#endregion

namespace RabbitLink.Topology
{
    /// <summary>
    ///     Topology config delegate for <see cref="ILinkConsumer" />
    /// </summary>
    public delegate Task<ILinkQueue> LinkConsumerTopologyConfigDelegate(ILinkTopologyConfig config);
}
