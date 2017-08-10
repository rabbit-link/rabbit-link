#region Usings

using System;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Topology
{
    /// <summary>
    /// Topology configuration handler
    /// </summary>
    public interface ILinkTopologyHandler
    {
        /// <summary>
        /// Configure handler
        /// </summary>
        Task Configure(ILinkTopologyConfig config);
        
        /// <summary>
        /// Ready handler
        /// </summary>
        Task Ready();
        
        /// <summary>
        /// Error handler
        /// </summary>
        Task ConfigurationError(Exception ex);
    }
}