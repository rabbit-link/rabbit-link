#region Usings

using System;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Topology
{
    public interface ILinkTopologyHandler
    {
        Task Configure(ILinkTopologyConfig config);
        Task Ready();
        Task ConfigurationError(Exception ex);
    }
}