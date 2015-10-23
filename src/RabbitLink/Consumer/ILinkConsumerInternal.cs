#region Usings

using System;

#endregion

namespace RabbitLink.Consumer
{
    internal interface ILinkConsumerInternal : ILinkConsumer
    {
        event EventHandler Disposed;
    }
}