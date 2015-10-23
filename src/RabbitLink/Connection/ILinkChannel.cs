#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Connection
{
    internal interface ILinkChannel : IDisposable
    {
        Guid Id { get; }
        bool IsOpen { get; }
        ILinkConnection Connection { get; }
        event EventHandler Ready;
        event EventHandler<ShutdownEventArgs> Shutdown;
        event EventHandler<FlowControlEventArgs> FlowControl;
        event EventHandler Recover;
        event EventHandler<BasicAckEventArgs> Ack;
        event EventHandler<BasicNackEventArgs> Nack;
        event EventHandler<BasicReturnEventArgs> Return;
        event EventHandler Disposed;
        Task InvokeActionAsync(Action<IModel> action, CancellationToken cancellation);
        Task InvokeActionAsync(Action<IModel> action);
    }
}