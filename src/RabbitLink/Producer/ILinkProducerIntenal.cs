#region Usings

using System;

#endregion

namespace RabbitLink.Producer
{
    internal interface ILinkProducerIntenal : ILinkProducer
    {
        event EventHandler Disposed;
    }
}