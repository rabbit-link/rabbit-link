#region Usings

using System;
using System.IO;
using System.Threading;
using RabbitLink.Async;
using RabbitLink.Configuration;
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
            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            ConnectionString = _configuration.ConnectionString;

            _connectionFactory = new ConnectionFactory
            {
                Uri = ConnectionString,
                TopologyRecoveryEnabled = false,
                AutomaticRecoveryEnabled = false,
                RequestedConnectionTimeout = (int)_configuration.ConnectionTimeout.TotalMilliseconds
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

            using (_syncLock.Lock())
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _logger.Debug("Disposing");

                _disposedCancellationSource.Cancel();
                _disposedCancellationSource.Dispose();
                Cleanup();

                _logger.Debug("Disposed");
                _logger.Dispose();
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region .fields

        private readonly AsyncLock _syncLock = new AsyncLock();
        private readonly ManualResetEventSlim _createModelEvent = new ManualResetEventSlim();
        private readonly object _timerSync = new object();

        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly CancellationToken _disposedCancellation;
        private readonly ConnectionFactory _connectionFactory;
        private readonly LinkConfiguration _configuration;

        private readonly ILinkLogger _logger;

        private IConnection _connection;
        private Timer _reconnectTimer;

        #endregion

        #region Events        

        public event EventHandler Disposed;
        public event EventHandler Connected;
        public event EventHandler<LinkDisconnectedEventArgs> Disconnected;

        #endregion

        #region Properties        

        public Guid Id { get; } = Guid.NewGuid();

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

            using (_syncLock.Lock())
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
                    _logger.Debug("Opening");

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
                _createModelEvent.Set();

                _logger.Info(
                    $"Connected (Host: {_connection.Endpoint.HostName}, Port: {_connection.Endpoint.Port}, LocalPort: {_connection.LocalPort})");
            }
        }

        private void ScheduleReconnect(bool wait)
        {
            if (IsConnected || _disposedCancellation.IsCancellationRequested)
                return;


            lock (_timerSync)
            {
                if (_reconnectTimer != null)
                {
                    _logger.Debug("Supressing reconnection due another one already running");
                    return;
                }

                var timeout = wait ? _configuration.ConnectionRecoveryInterval : TimeSpan.Zero;
                _logger.Info($"Reconnecting in {timeout.TotalSeconds:0.###}s");
                _reconnectTimer = new Timer(OnReconnectTimerFired, null, timeout, TimeSpan.FromMilliseconds(-1));
            }
        }

        private void OnReconnectTimerFired(object state)
        {
            Connect();
        }

        private void Cleanup()
        {
            lock (_timerSync)
            {
                _reconnectTimer?.Dispose();
                _reconnectTimer = null;
            }

            _createModelEvent.Reset();

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

            try
            {
                using (_syncLock.Lock(_disposedCancellation))
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
            catch (OperationCanceledException)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                throw;
            }
        }

        public IModel CreateModel(CancellationToken cancellationToken)
        {
            using (var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                _disposedCancellation))
            {
                if (compositeCancellation.IsCancellationRequested)
                {
                    if (_disposedCancellation.IsCancellationRequested)
                        throw new ObjectDisposedException(GetType().Name);

                    cancellationToken.ThrowIfCancellationRequested();
                }

                try
                {
                    while (true)
                    {
                        _createModelEvent.Wait(compositeCancellation.Token);
                        using (_syncLock.Lock(compositeCancellation.Token))
                        {
                            if (!IsConnected)
                                continue;

                            return _connection.CreateModel();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (_disposedCancellation.IsCancellationRequested)
                        throw new ObjectDisposedException(GetType().Name);

                    throw;
                }
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

            _createModelEvent.Reset();
            OnDisconnected(e);

            // if initialized by application, exit
            if (e.Initiator == ShutdownInitiator.Application) return;

            ScheduleReconnect(true);
        }

        #endregion
    }
}