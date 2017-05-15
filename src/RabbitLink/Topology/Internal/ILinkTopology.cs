#region Usings

using System;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal interface ILinkTopology : IDisposable
    {
        Guid Id { get; }
        LinkTopologyState State { get; }
        event EventHandler Disposed;
    }
}