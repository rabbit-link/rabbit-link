using System;

namespace RabbitLink.Consumer
{
    internal interface ILinkConsumerInternal : ILinkConsumer
    {
        event EventHandler Disposed;
    }
}