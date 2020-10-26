#region Usings

using System;
using RabbitLink.Connection;

#endregion

namespace RabbitLink.Producer
{
    internal interface ILinkProducerInternal : ILinkProducer
    {
        event EventHandler Disposed;

        ILinkChannel Channel { get; }
    }
}
