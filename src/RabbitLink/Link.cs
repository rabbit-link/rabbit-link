#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Connection;
using RabbitLink.Consumer;
using RabbitLink.Topology;

#endregion

namespace RabbitLink
{
    /// <summary>
    /// Represents connection to RabbitMQ
    /// </summary>
    internal sealed class Link : ILink
    {
        private readonly object _sync = new object();
        
        #region Ctor

        /// <summary>
        /// Creates new <see cref="Link"/> instance
        /// </summary>
        public Link(LinkConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new LinkConnection(_configuration);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Is Link connected
        /// </summary>
        public bool IsConnected => !_disposed && _connection.State == LinkConnectionState.Active;

        #endregion

        #region IDisposable and Finalizer

        /// <summary>
        /// Cleaning up connection and all dependent resources
        /// </summary>
        public void Dispose()
            => Dispose(true);

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
        /// Finalizer
        /// </summary>
        ~Link()
            => Dispose(false);

        #endregion

        internal ILinkChannel CreateChannel(LinkStateHandler<LinkChannelState> stateHandler, TimeSpan recoveryInterval)
        {
            return new LinkChannel(_connection, stateHandler, recoveryInterval);
        }

        /// <summary>
        /// Initializes connection
        /// </summary>
        public void Initialize()
            => _connection.Initialize();


        public ILinkProducerBuilder Producer => new LinkProducerBuilder(this, _configuration.RecoveryInterval);
        public ILinkTopologyBuilder Topology => new LinkTopologyBuilder(this, _configuration.RecoveryInterval);

        #region Fields

        private readonly LinkConfiguration _configuration;
        private readonly ILinkConnection _connection;
        private bool _disposed;

        #endregion

        #region Consumer 

        public ILinkConsumer CreateConsumer(
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Func<Exception, Task> configurationError = null,
            Action<ILinkConsumerConfigurationBuilder> config = null
        )
        {
            if (topologyConfiguration == null)
                throw new ArgumentNullException(nameof(topologyConfiguration));

            if (configurationError == null)
            {
                configurationError = ex => Task.FromResult((object) null);
            }

            var configBuilder = new LinkConsumerConfigurationBuilder(_configuration);
            config?.Invoke(configBuilder);

            throw new NotImplementedException();

            //return new LinkConsumer(configBuilder.Configuration, _configuration, CreateChannel(),
            //    topologyConfiguration, configurationError);
        }

        #endregion

        #region Events

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
    }
}