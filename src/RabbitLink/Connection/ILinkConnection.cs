#region Usings

using System;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Connection
{
    internal interface ILinkConnection : IDisposable
    {
        bool IsConnected { get; }
        bool Initialized { get; }
        event EventHandler Disposed;
        event EventHandler Connected;
        event EventHandler<LinkDisconnectedEventArgs> Disconnected;
        void Initialize();
        IModel CreateModel();
    }
}