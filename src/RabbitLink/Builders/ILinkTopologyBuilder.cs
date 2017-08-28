using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Connection;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;

namespace RabbitLink.Builders
{
    /// <summary>
    /// Builder for <see cref="ILinkTopology"/>
    /// </summary>
    public interface ILinkTopologyBuilder
    {
        /// <summary>
        /// Channel / Topology recovery interval
        /// By default <see cref="ILinkBuilder.RecoveryInterval"/>
        /// </summary>
        ILinkTopologyBuilder RecoveryInterval(TimeSpan value);
        
        /// <summary>
        /// Sets handler for state changes
        /// </summary>
        ILinkTopologyBuilder OnStateChange(LinkStateHandler<LinkTopologyState> handler);
        
        /// <summary>
        /// Sets handler for channel state changes
        /// </summary>
        ILinkTopologyBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> handler);

        /// <summary>
        /// Sets topology handler
        ///</summary>
        ILinkTopologyBuilder Handler(ILinkTopologyHandler handler);
        
        /// <summary>
        /// Sets topology configuration handler
        /// </summary>
        ILinkTopologyBuilder Handler(LinkTopologyConfigDelegate config);
        
        /// <summary>
        /// Sets topology configuration and ready handlers
        /// </summary>
        ILinkTopologyBuilder Handler(LinkTopologyConfigDelegate config, LinkTopologyReadyDelegate ready);

        /// <summary>
        /// Sets topology configuration, ready and error handlers
        /// </summary>
        ILinkTopologyBuilder Handler(
            LinkTopologyConfigDelegate config,
            LinkTopologyReadyDelegate ready,
            LinkTopologyErrorDelegate error
        );
        
        /// <summary>
        /// Builds <see cref="ILinkTopology"/> instance
        /// </summary>
        ILinkTopology Build();

        /// <summary>
        /// Builds <see cref="ILinkTopology"/> instance, waits when it will be ready and then dispose it
        /// </summary>
        Task WaitAsync(CancellationToken? cancellation = null);
    }
}