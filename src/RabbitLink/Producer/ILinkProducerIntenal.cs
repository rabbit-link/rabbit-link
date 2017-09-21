#region Usings

using System;
using RabbitLink.Connection;

#endregion

namespace RabbitLink.Producer
{
    internal interface ILinkProducerIntenal : ILinkProducer
    {
        event EventHandler Disposed;

        ILinkChannel Channel { get; }
    }
}
