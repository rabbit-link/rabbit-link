using System;

namespace RabbitLink.Topology.Internal
{
    internal interface ILinkTopologyInternal : ILinkTopology
    {
        event EventHandler Disposed;
    }
}