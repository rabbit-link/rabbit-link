#region Usings

using System.Threading.Tasks;

#endregion

namespace RabbitLink.Topology
{
    /// <summary>
    ///     Ready handler for <see cref="ILinkTopology" />
    /// </summary>
    public delegate Task LinkTopologyReadyDelegate();
}
