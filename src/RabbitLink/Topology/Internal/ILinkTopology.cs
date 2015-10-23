#region Usings

using System;
using RabbitLink.Connection;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal interface ILinkTopology : IDisposable
    {
        Guid Id { get; }
        bool Configured { get; }
        ILinkChannel Channel { get; }
        void ScheduleConfiguration(bool delay = false);
        event EventHandler Disposed;
    }
}