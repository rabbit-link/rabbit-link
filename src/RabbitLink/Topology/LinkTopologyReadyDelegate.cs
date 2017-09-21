using System.Threading.Tasks;

namespace RabbitLink.Topology
{
    /// <summary>
    /// Ready handler for <see cref="ILinkTopology"/>
    /// </summary>
    public delegate Task LinkTopologyReadyDelegate();
}
