using System;

namespace RabbitLink.Consumer
{
    internal interface ILinkPushConsumerInternal : ILinkPushConsumer
    {
        event EventHandler Disposed;
    }
}