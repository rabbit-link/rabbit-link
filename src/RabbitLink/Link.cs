#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Connection;
using RabbitLink.Consumer;
using RabbitLink.Producer;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;

#endregion

namespace RabbitLink
{
    /// <summary>
    /// Represents connection to RabbitMQ
    /// </summary>
    internal sealed class Link : ILink
    {
        #region Ctor
        /// <summary>
        /// Creates new <see cref="Link"/> instance
        /// </summary>
        public Link(LinkConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            _connection.Dispose();
            _disposed = true;

            if (!disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Link()
        {
            Dispose(false);
        }

        #endregion

        private ILinkChannel CreateChannel()
        {
            return new LinkChannel(_configuration, _connection);
        }

        /// <summary>
        /// Initializes connection
        /// </summary>
        public void Initialize()
        {
            _connection.Initialize();
        }

        #region Producer

        public ILinkProducer CreateProducer(
            ILinkProducerTopologyHandler topologyHandler,
            Action<ILinkProducerConfigurationBuilder> config = null
            )
        {
            if (topologyHandler == null)
                throw new ArgumentNullException(nameof(topologyHandler));

            var configBuilder = new LinkProducerBuilder(_configuration);
            config?.Invoke(configBuilder);

            return new LinkProducer(configBuilder.Configuration, _configuration, CreateChannel(), topologyConfiguration,
                configurationError);
        }

        #endregion

        #region Fields

        private readonly LinkConfiguration _configuration;
        private readonly ILinkConnection _connection;
        private bool _disposed;

        #endregion

        #region Topology configurators

        public IDisposable CreatePersistentTopologyConfigurator(ILinkTopologyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkTopology(_configuration, CreateChannel(), handler, false);
        }

        public IDisposable CreateTopologyConfigurator(ILinkTopologyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkTopology(_configuration, CreateChannel(), handler, true);
        }

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