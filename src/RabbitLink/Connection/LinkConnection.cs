#region Usings

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Configuration;
using RabbitLink.Exceptions;
using RabbitLink.Internals;
using RabbitLink.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Connection
{
    internal class LinkConnection : ILinkConnection
    {
        #region .ctor

        public LinkConnection(LinkConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;
            _logger = _configuration.LoggerFactory.CreateLogger(GetType().Name);

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            ConnectionString = _configuration.ConnectionString;

            _connectionFactory = new ConnectionFactory
            {
                Uri = ConnectionString,
                TopologyRecoveryEnabled = false,
                AutomaticRecoveryEnabled = false,
                RequestedConnectionTimeout = (int) _configuration.ConnectionTimeout.TotalMilliseconds
            };

            UserId = _connectionFactory.UserName;

            _disposedCancellationSource = new CancellationTokenSource();
            _disposedCancellation = _disposedCancellationSource.Token;

            _logger.Debug("Created");
            if (_configuration.AutoStart)
            {
                Initialize();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _logger.Debug("Disposing");

                _disposedCancellationSource.Cancel();
                _disposedCancellationSource.Dispose();
                _eventLoop.Dispose();
                Cleanup();

                _logger.Debug("Disposed");
                _logger.Dispose();
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Fields

        private readonly object _sync = new object();
        private readonly object _connectionSync = new object();
        private readonly EventLoop _eventLoop = new EventLoop();
        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly CancellationToken _disposedCancellation;
        private readonly ConnectionFactory _connectionFactory;
        private readonly LinkConfiguration _configuration;

        private readonly ILinkLogger _logger;

        private IConnection _connection;

        #endregion

        #region Events        

        public event EventHandler Disposed;
        public event EventHandler Connected;
        public event EventHandler<LinkDisconnectedEventArgs> Disconnected;

        #endregion

        #region Properties        

        public bool IsConnected => !_disposedCancellation.IsCancellationRequested &&
                                   Initialized &&
                                   _connection?.IsOpen == true;

        public bool Initialized { get; private set; }

        public string ConnectionString { get; }
        public string UserId { get; }

        #endregion

        #region Connection management

        private void Connect()
        {
            // if already connected or cancelled
            if (IsConnected || _disposedCancellation.IsCancellationRequested)
                return;

            lock (_connectionSync)
            {
                // Second check
                if (IsConnected || _disposedCancellation.IsCancellationRequested)
                    return;

                _logger.Info("Connecting");

                // Cleaning old connection
                Cleanup();

                // Last chance to cancel
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                try
                {
                    _logger.Debug($"Opening");

                    _connection = _connectionFactory.CreateConnection();
                    _connection.ConnectionShutdown += ConnectionOnConnectionShutdown;
                    _connection.CallbackException += ConnectionOnCallbackException;
                    _connection.ConnectionBlocked += ConnectionOnConnectionBlocked;
                    _connection.ConnectionUnblocked += ConnectionOnConnectionUnblocked;

                    _logger.Debug("Sucessfully opened");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Cannot connect: {ex.Message}");
                    ScheduleReconnect(true);
                    return;
                }

                Connected?.Invoke(this, EventArgs.Empty);

                _logger.Info("Connected");
            }
        }

        private void ScheduleReconnect(bool wait)
        {
            if (IsConnected || _disposedCancellation.IsCancellationRequested)
                return;

            try
            {
                _eventLoop.ScheduleAsync(async () =>
                {
                    if (wait)
                    {
                        _logger.Info($"Reconnecting in {_configuration.ConnectionRecoveryInterval.TotalSeconds:0.###}s");
                        await Task.Delay(_configuration.ConnectionRecoveryInterval, _disposedCancellation)
                            .ConfigureAwait(false);
                    }

                    Connect();
                }, _disposedCancellation);
            }
            catch
            {
                // no op
            }
        }

        private void Cleanup()
        {
            try
            {
                _connection?.Dispose();
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                _logger.Warning("Cleaning exception: {0}", ex);
            }
        }

        private void OnDisconnected(ShutdownEventArgs e)
        {
            _logger.Debug("Invoking Disconnected event");

            LinkDisconnectedInitiator initiator;

            switch (e.Initiator)
            {
                case ShutdownInitiator.Application:
                    initiator = LinkDisconnectedInitiator.Application;
                    break;
                case ShutdownInitiator.Library:
                    initiator = LinkDisconnectedInitiator.Library;
                    break;
                case ShutdownInitiator.Peer:
                    initiator = LinkDisconnectedInitiator.Peer;
                    break;
                default:
                    initiator = LinkDisconnectedInitiator.Library;
                    break;
            }

            var eventArgs = new LinkDisconnectedEventArgs(initiator, e.ReplyCode, e.ReplyText);
            Disconnected?.Invoke(this, eventArgs);

            _logger.Debug("Disconnected event sucessfully invoked");
        }

        #endregion

        #region Public methods

        public void Initialize()
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (Initialized)
                return;

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (Initialized) return;

                _logger.Debug("Initializing");
                Initialized = true;

                ScheduleReconnect(false);
                _logger.Debug("Initialized");
            }
        }

        public IModel CreateModel()
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (!IsConnected)
                throw new LinkNotConnectedException();

            lock (_connectionSync)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (!IsConnected)
                    throw new LinkNotConnectedException();

                return _connection.CreateModel();
            }
        }

        #endregion

        #region Connection event handlers

        private void ConnectionOnConnectionUnblocked(object sender, EventArgs e)
        {
            _logger.Debug("Unblocked");
        }

        private void ConnectionOnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            _logger.Debug($"Blocked, reason: {e.Reason}");
        }

        private void ConnectionOnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            _logger.Error($"Callback exception: {e.Exception}");
        }

        private void ConnectionOnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.Info($"Diconnected, Initiator: {e.Initiator}, Code: {e.ReplyCode}, Message: {e.ReplyText}");
            OnDisconnected(e);

            // if initialized by application, exit
            if (e.Initiator == ShutdownInitiator.Application) return;

            ScheduleReconnect(true);
        }

        #endregion
    }
}