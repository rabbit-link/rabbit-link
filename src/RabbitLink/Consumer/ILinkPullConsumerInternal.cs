#region Usings

using System;

#endregion

namespace RabbitLink.Consumer
{
    internal interface ILinkPullConsumerInternal : ILinkPullConsumer
    {
        event EventHandler Disposed;
    }
}