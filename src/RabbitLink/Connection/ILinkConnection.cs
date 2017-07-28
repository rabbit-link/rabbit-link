#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
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
        /// Identifier
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Operational state
        /// </summary>
        LinkConnectionState State { get; }

        /// <summary>
        /// Id of user who initialize connection
        /// </summary>
        string UserId { get; }

        #endregion

        /// <summary>
        /// Emitted when disposed
        /// </summary>
        event EventHandler Disposed;
        
        /// <summary>
        /// Emmited when connected
        /// </summary>
        event EventHandler Connected;
        
        /// <summary>
        /// Emmited when disconnected
        /// </summary>
        event EventHandler Disconnected;

        /// <summary>
        /// Initialize connection
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Create <see cref="IModel"/>
        /// </summary>
        /// <param name="cancellation">Action cancellation</param>
        Task<IModel> CreateModelAsync(CancellationToken cancellation);
        
        /// <summary>
        /// Configuration
        /// </summary>
        LinkConfiguration Configuration { get; }
    }
}