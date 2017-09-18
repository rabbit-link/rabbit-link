#region Usings

using System;
using RabbitLink.Builders;
using RabbitLink.Connection;

#endregion

namespace RabbitLink
{
    /// <inheritdoc />
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

        #region ILink Members

        public bool IsConnected 
            => !_disposed && _connection.State == LinkConnectionState.Active;

        public void Dispose() 
            => Dispose(true);

        public void Initialize() 
            => _connection.Initialize();


        public ILinkProducerBuilder Producer => 
            new LinkProducerBuilder(this, _configuration.RecoveryInterval, _configuration.Serializer);
        
        public ILinkConsumerBuilder Consumer => 
            new LinkConsumerBuilder(this, _configuration.RecoveryInterval, _configuration.Serializer);
        
        public ILinkTopologyBuilder Topology => 
            new LinkTopologyBuilder(this, _configuration.RecoveryInterval);

        public event EventHandler Connected
        {
            add => _connection.Connected += value;
            remove => _connection.Connected -= value;
        }

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

        ~Link() 
            => Dispose(false);

        internal ILinkChannel CreateChannel(LinkStateHandler<LinkChannelState> stateHandler, TimeSpan recoveryInterval)
        {
            return new LinkChannel(_connection, stateHandler, recoveryInterval);
        }
    }
}