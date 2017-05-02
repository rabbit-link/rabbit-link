#region Usings

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        #region Fields

        private readonly LinkConfiguration _configuration;
        private readonly ConnectionFactory _connectionFactory;
        private readonly CancellationToken _disposedCancellation;

        private readonly CancellationTokenSource _disposedCancellationSource;

        private readonly ILinkLogger _logger;

        private readonly AsyncLock _syncLock = new AsyncLock();
        private readonly SemaphoreSlim _connectedSem = new SemaphoreSlim(0);
        private readonly object _timerSync = new object();

        private IConnection _connection;
        private Timer _reconnectTimer;

        #endregion

        #region Ctor

        public LinkConnection(LinkConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            ConnectionString = _configuration.ConnectionString;

            _connectionFactory = new ConnectionFactory
            {
                Uri = ConnectionString,
                TopologyRecoveryEnabled = false,
                AutomaticRecoveryEnabled = false,
                RequestedConnectionTimeout = (int) _configuration.ConnectionTimeout.TotalMilliseconds,
                ClientProperties =
                {
                    ["product"] = "RabbitLink",
                    ["version"] = GetType().GetTypeInfo().Assembly.GetName().Version.ToString(3),
                    ["copyright"] = "Copyright (c) 2015-2017 RabbitLink",
                    ["information"] = "https://github.com/rabbit-link/rabbit-link",
                    ["app_id"] = configuration.AppId
                }
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

        #region ILinkConnection Members

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

        public event EventHandler Disposed;
        public event EventHandler Connected;
        public event EventHandler<LinkDisconnectedEventArgs> Disconnected;

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsConnected => !_disposedCancellation.IsCancellationRequested &&
                                   Initialized &&
                                   _connection?.IsOpen == true;

        public bool Initialized { get; private set; }

        public string ConnectionString { get; }
        public string UserId { get; }

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

        public async Task<IModel> CreateModelWaitAsync(TimeSpan waitInterval, CancellationToken cancellationToken)
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

                if (waitInterval > TimeSpan.Zero && IsConnected)
                {
                    await Task.Delay(waitInterval, compositeCancellation.Token)
                        .ConfigureAwait(false);
                }

                try
                {
                    while (true)
                    {
                        await _connectedSem.WaitAsync(compositeCancellation.Token).ConfigureAwait(false);

                        try
                        {
                            using (await _syncLock.LockAsync(compositeCancellation.Token).ConfigureAwait(false))
                            {
                                if (!IsConnected)
                                    continue;

                                return _connection.CreateModel();
                            }
                        }
                        finally
                        {
                            if (IsConnected)
                            {
                                _connectedSem.Release();
                            }
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


                _connectedSem.Release();
                Connected?.Invoke(this, EventArgs.Empty);

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
    }
}