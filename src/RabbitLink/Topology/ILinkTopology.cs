#region Usings

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Topology
{
    /// <summary>
    /// Represents RabbitMQ topology configurator
    /// </summary>
    public interface ILinkTopology : IDisposable
    {
        /// <summary>
        /// Id
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Operational state
        /// </summary>
        LinkTopologyState State { get; }

        /// <summary>
        /// Waits for topology ready
        /// </summary>
        Task WaitReadyAsync(CancellationToken? cancellation = null);
    }
}
