#region Usings

using System;
using System.Threading;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Connection
{
    internal interface ILinkConnection : IDisposable
    {
        Guid Id { get; }
        bool IsConnected { get; }
        bool Initialized { get; }
        string ConnectionString { get; }
        string UserId { get; }
        event EventHandler Disposed;
        event EventHandler Connected;
        event EventHandler<LinkDisconnectedEventArgs> Disconnected;
        void Initialize();
        IModel CreateModel(CancellationToken cancellationToken);
    }
}