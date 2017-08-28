using System;
using RabbitLink.Connection;

namespace RabbitLink.Consumer
{
    internal interface ILinkConsumerInternal : ILinkConsumer
    {
        event EventHandler Disposed;

        ILinkChannel Channel { get; }
    }
}