#region Usings

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Configuration;
using RabbitLink.Internals.Queues;
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
        private readonly ILinkConnectionFactory _connectionFactory;
        private readonly ILinkLogger _logger;

        private readonly CancellationToken _disposeCancellation;
        private readonly CancellationTokenSource _disposeCts;
        private readonly QueueRunner<IConnection> _runner = new QueueRunner<IConnection>();

        private readonly object _sync = new object();

        private IConnection _connection;

        private Task _loopTask;
        private CancellationTokenSource _connectionActiveCts;

        #endregion

        #region Ctor

        public LinkConnection(LinkConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            _connectionFactory = new LinkConnectionFactory(
                "default",
                _configuration.AppId,
                _configuration.ConnectionString,
                _configuration.ConnectionTimeout
                );

            _disposeCts = new CancellationTokenSource();
            _disposeCancellation = _disposeCts.Token;

            _logger.Debug("Created");
            if (_configuration.AutoStart)
            {
                Initialize();
            }
        }

        #endregion

        public void Dispose()
        {
            if (State == LinkConnectionState.Disposed)
                return;

            lock (_sync)
            {
                if (State == LinkConnectionState.Disposed)
                    return;

                _logger.Debug("Disposing");

                _disposeCts.Cancel();
                _disposeCts.Dispose();

                try
                {
                    _loopTask?.Wait(CancellationToken.None);
                }
                catch
                {
                    // no op
                }

                State = LinkConnectionState.Disposed;

                _runner.Dispose(new ObjectDisposedException(GetType().Name));

                Disposed?.Invoke(this, EventArgs.Empty);

                _logger.Debug("Disposed");
                _logger.Dispose();
            }
        }

        public event EventHandler Disposed;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public Guid Id { get; } = Guid.NewGuid();
        public LinkConnectionState State { get; private set; }

        public string UserId => _connectionFactory.UserName;

        public void Initialize()
        {
            if (_disposeCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (State != LinkConnectionState.Init)
                throw new InvalidOperationException("Already initialized");

            lock (_sync)
            {
                if (_disposeCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (State != LinkConnectionState.Init)
                    throw new InvalidOperationException("Already initialized");

                State = LinkConnectionState.Open;
                _loopTask = Task.Run(async () => await Loop().ConfigureAwait(false), _disposeCancellation);
            }
        }

        public Task<IModel> CreateModelAsync(CancellationToken cancellation)
        {
            return _runner.EnqueueAsync(conn => conn.CreateModel(), cancellation);
        }

        #region Loop

        private async Task Loop()
        {
            var newState = State;

            while (true)
            {
                if (_disposeCancellation.IsCancellationRequested)
                {
                    newState = LinkConnectionState.Stop;
                }

                if (newState != State)
                {
                    _logger.Debug($"State change {State} -> {newState}");
                    State = newState;
                }

                try
                {
                    switch (State)
                    {
                        case LinkConnectionState.Open:
                        case LinkConnectionState.Reopen:
                            newState = await OnOpenReopenAsync(State == LinkConnectionState.Reopen)
                                .ConfigureAwait(false);
                            break;
                        case LinkConnectionState.Active:
                            await OnActiveAsync()
                                .ConfigureAwait(false);
                            newState = LinkConnectionState.Stop;
                            break;
                        case LinkConnectionState.Stop:
                            newState = await OnStopAsync()
                                .ConfigureAwait(false);
                            break;
                        case LinkConnectionState.Disposed:
                            return;

                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unhandled exception: {ex}");
                }
            }
        }

        #region Actions

        private async Task<LinkConnectionState> OnOpenReopenAsync(bool reopen)
        {
            var newState = LinkConnectionState.Stop;

            using (var yieldCts = new CancellationTokenSource())
            {
                var cts = yieldCts;

                var connectTask = Task.Run(async () =>
                {
                    try
                    {
                        if (await ConnectAsync(reopen).ConfigureAwait(false))
                        {
                            newState = LinkConnectionState.Active;
                        }
                    }
                    finally
                    {
                        cts.Cancel();
                    }
                }, CancellationToken.None);

                try
                {
                    await _runner.YieldAsync(cts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // No Op
                }

                await connectTask
                    .ConfigureAwait(false);
            }

            return newState;
        }

        #region Connect

        private async Task<bool> ConnectAsync(bool reopen)
        {
            if (_disposeCancellation.IsCancellationRequested)
                return false;

            if (reopen)
            {
                var timeout = _configuration.ConnectionRecoveryInterval;
                _logger.Info($"Reopening in {timeout.TotalSeconds:0.###}s");

                try
                {
                    await Task.Delay(timeout, _disposeCancellation)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }

            // start long-running task for syncronyous connect
            return await Task.Factory
                .StartNew(Connect, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private bool Connect()
        {
            if (_disposeCancellation.IsCancellationRequested)
                return false;

            _logger.Info("Connecting");

            try
            {
                _connection = _connectionFactory.GetConnection();
                _connectionActiveCts = new CancellationTokenSource();

                _connection.ConnectionShutdown += ConnectionOnConnectionShutdown;
                _connection.CallbackException += ConnectionOnCallbackException;
                _connection.ConnectionBlocked += ConnectionOnConnectionBlocked;
                _connection.ConnectionUnblocked += ConnectionOnConnectionUnblocked;
            }
            catch (Exception ex)
            {
                _logger.Error($"Cannot connect: {ex.Message}");
                return false;
            }

            _logger.Info(
                $"Connected (Host: {_connection.Endpoint.HostName}, Port: {_connection.Endpoint.Port}, LocalPort: {_connection.LocalPort})");

            return true;
        }

        #endregion

        private Task<LinkConnectionState> OnStopAsync()
        {
            _connectionActiveCts?.Cancel();
            _connectionActiveCts?.Dispose();

            try
            {
                _connection?.Dispose();
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                _logger.Warning($"Cleaning exception: {ex}");
            }

            return Task.FromResult(_disposeCancellation.IsCancellationRequested
                ? LinkConnectionState.Disposed
                : LinkConnectionState.Reopen
            );
        }

        private Task OnActiveAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                Connected?.Invoke(this, EventArgs.Empty);

                using (var cts =
                    CancellationTokenSource.CreateLinkedTokenSource(_disposeCancellation, _connectionActiveCts.Token))
                {
                    try
                    {
                       _runner.Run(_connection, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Processing stopped: {ex}");
                    }
                }

                Disconnected?.Invoke(this, EventArgs.Empty);
            }, TaskCreationOptions.LongRunning);
        }

        #endregion

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

            // if initialized by application, exit
            if (e.Initiator == ShutdownInitiator.Application) return;

            _connectionActiveCts?.Cancel();
        }

        #endregion
    }
}