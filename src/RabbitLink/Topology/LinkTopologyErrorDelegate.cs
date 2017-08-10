using System;
using System.Threading.Tasks;

namespace RabbitLink.Topology
{
    /// <summary>
    /// Error handler for topology configuration
    /// </summary>
    public delegate Task LinkTopologyErrorDelegate(Exception ex);
}