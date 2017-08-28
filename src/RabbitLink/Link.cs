#region Usings

using System;
using RabbitLink.Builders;
using RabbitLink.Connection;

#endregion

namespace RabbitLink
{
    /// <summary>
    ///     Represents connection to RabbitMQ
    /// </summary>
    internal sealed class Link : ILink
    {
        #region Fields

        private readonly LinkConfiguration _configuration;
        private readonly ILinkConnection _connection;
        private readonly object _sync = new object();
        private bool _disposed;

        #endregion

        #region Ctor

        /// <summary>
        ///     Creates new <see cref="Link" /> instance
        /// </summary>
        public Link(LinkConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new LinkConnection(_configuration);
        }

        #endregion

        #region Properties

        public ILinkConsumerBuilder Consumer => new LinkConsumerBuilder();

        #endregion

        #region ILink Members

        /// <summary>
        ///     Is Link connected
        /// </summary>
        public bool IsConnected 
            => !_disposed && _connection.State == LinkConnectionState.Active;

        /// <summary>
        ///     Cleaning up connection and all dependent resources
        /// </summary>
        public void Dispose() 
            => Dispose(true);

        /// <summary>
        ///     Initializes connection
        /// </summary>
        public void Initialize() 
            => _connection.Initialize();


        public ILinkProducerBuilder Producer => new LinkProducerBuilder(this, _configuration.RecoveryInterval);
        public ILinkTopologyBuilder Topology => new LinkTopologyBuilder(this, _configuration.RecoveryInterval);

        /// <summary>
        ///     Invokes when connected, must not perform blocking operations.
        /// </summary>
        public event EventHandler Connected
        {
            add => _connection.Connected += value;
            remove => _connection.Connected -= value;
        }

        /// <summary>
        ///     Invokes when disconnected, must not perform blocking operations.
        /// </summary>
        public event EventHandler Disconnected
        {
            add => _connection.Disconnected += value;
            remove => _connection.Disconnected -= value;
        }

        #endregion

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                if (_disposed)
                    return;

                _connection.Dispose();
                _disposed = true;

                if (disposing)
                    GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        ///     Finalizer
        /// </summary>
        ~Link() 
            => Dispose(false);

        internal ILinkChannel CreateChannel(LinkStateHandler<LinkChannelState> stateHandler, TimeSpan recoveryInterval)
        {
            return new LinkChannel(_connection, stateHandler, recoveryInterval);
        }
    }
}