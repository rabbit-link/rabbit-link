#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Connection
{
    /// <summary>
    ///     Represents <see cref="IConnection"/> with automatic recovering
    /// </summary>
    internal interface ILinkConnection : IDisposable
    {
        #region Properties

        /// <summary>
        ///     Identifier
        /// </summary>
        Guid Id { get; }

        /// <summary>
        ///     Operational state
        /// </summary>
        LinkConnectionState State { get; }

        string UserId { get; }

        #endregion

        event EventHandler Disposed;
        event EventHandler Connected;
        event EventHandler Disconnected;

        void Initialize();
        Task<IModel> CreateModelAsync(CancellationToken cancellationToken);
    }
}