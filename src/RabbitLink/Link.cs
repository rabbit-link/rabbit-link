#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Configuration;
using RabbitLink.Connection;
using RabbitLink.Consumer;
using RabbitLink.Messaging;
using RabbitLink.Producer;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;

#endregion

namespace RabbitLink
{
    public sealed class Link : IDisposable
    {
        #region Ctor

        public Link(string connectionString, Action<ILinkConfigurationBuilder> config = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            var configBuilder = new LinkConfigurationBuilder();
            config?.Invoke(configBuilder);

            _configuration = configBuilder.Configuration;
            _configuration.ConnectionString = connectionString;

            _connection = new LinkConnection(_configuration);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Is Link connected
        /// </summary>
        public bool IsConnected => !_disposed && _connection.IsConnected;                              

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (_disposed) return;
            _connection.Dispose();
            _disposed = true;
        }

        #endregion

        private ILinkChannel CreateChannel()
        {
            return new LinkChannel(_configuration, _connection);
        }

        public void Initialize()
        {
            _connection.Initialize();
        }

        #region Producer

        public ILinkProducer CreateProducer(
            Func<ILinkTopologyConfig, Task<ILinkExchage>> topologyConfiguration,
            Func<Exception, Task> configurationError = null,
            Action<ILinkProducerConfigurationBuilder> config = null
            )
        {
            if (topologyConfiguration == null)
                throw new ArgumentNullException(nameof(topologyConfiguration));

            if (configurationError == null)
            {
                configurationError = ex => Task.FromResult((object) null);
            }

            var configBuilder = new LinkProducerConfigurationBuilder(_configuration);
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

        public ILinkPullConsumer CreatePullConsumer(
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Func<Exception, Task> configurationError = null,
            Action<ILinkPullConsumerConfigurationBuilder> config = null
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

            return new LinkPullConsumer(configBuilder.Configuration, _configuration, CreateChannel(),
                topologyConfiguration, configurationError);
        }

        public ILinkPushConsumer CreatePushConsumer(
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Action<ILinkConsumerHandlerConfiguration> handlerConfiguration,
            Func<Exception, Task> configurationError = null,
            Action<Exception, ILinkRecievedMessage<byte[]>> serializationError = null,
            Action<ILinkPushConsumerConfigurationBuilder> config = null
            )
        {
            if (topologyConfiguration == null)
                throw new ArgumentNullException(nameof(topologyConfiguration));

            if (configurationError == null)
            {
                configurationError = ex => Task.FromResult((object) null);
            }

            if (serializationError == null)
            {
                serializationError = (exception, message) => { };
            }

            if (handlerConfiguration == null)
                throw new ArgumentNullException(nameof(handlerConfiguration));

            var configBuilder = new LinkConsumerConfigurationBuilder(_configuration);
            config?.Invoke(configBuilder);

            var handlerBuilder = new LinkConsumerHandlerConfiguration();
            handlerConfiguration(handlerBuilder);

            var handlerSearchFunc = handlerBuilder.Build();

            var underlyingConsumer = new LinkPullConsumer(
                configBuilder.Configuration,
                _configuration,
                CreateChannel(),
                topologyConfiguration,
                configurationError
                );

            return new LinkPushConsumer(
                underlyingConsumer,
                configBuilder.Configuration,
                _configuration,
                handlerSearchFunc,
                serializationError
                );
        }

        #endregion

        #region Events

        /// <summary>
        ///     Invokes when connected, must not perform blocking operations.
        /// </summary>
        public event EventHandler Connected
        {
            add { _connection.Connected += value; }
            remove { _connection.Connected -= value; }
        }

        /// <summary>
        ///     Invokes when disconnected, must not perform blocking operations.
        /// </summary>
        public event EventHandler<LinkDisconnectedEventArgs> Disconnected
        {
            add { _connection.Disconnected += value; }
            remove { _connection.Disconnected -= value; }
        }

        #endregion
    }
}