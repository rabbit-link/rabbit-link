#region Usings

using System;

#endregion

namespace RabbitLink.Consumer
{
    internal interface ILinkPushConsumerInternal : ILinkPushConsumer
    {
        event EventHandler Disposed;
    }
}