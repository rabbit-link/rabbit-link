#region Usings

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Internals;
using RabbitLink.Internals.Actions;
using RabbitLink.Internals.Async;
using RabbitLink.Internals.Queues;
using RabbitLink.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Connection
{
    internal class LinkConnection : AsyncStateMachine<LinkConnectionState>, ILinkConnection
    {
        #region Fields

        private readonly LinkConfiguration _configuration;
        private readonly ILinkConnectionFactory _connectionFactory;

        private readonly CancellationToken _disposeCancellation;
        private readonly CancellationTokenSource _disposeCts;
        private readonly ILinkLogger _logger;
        private readonly CompositeActionStorage<IConnection> _storage = new CompositeActionStorage<IConnection>();

        private readonly object _sync = new object();

        private IConnection _connection;
        private CancellationTokenSource _connectionActiveCts;

        private Task _loopTask;

        #endregion

        #region Ctor

        public LinkConnection(LinkConfiguration configuration) : base(LinkConnectionState.Init)
        {
            _configuration = configuration;

            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            _connectionFactory = new LinkConnectionFactory(
                "default",
                _configuration.AppId,
                _configuration.ConnectionString,
                _configuration.Timeout
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

        #region ILinkConnection Members

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

                var ex = new ObjectDisposedException(GetType().Name);
                _storage.Complete(item => item.TrySetException(ex));

                ChangeState(LinkConnectionState.Disposed);

                Disposed?.Invoke(this, EventArgs.Empty);

                _logger.Debug("Disposed");
                _logger.Dispose();
            }
        }

        public event EventHandler Disposed;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public Guid Id { get; } = Guid.NewGuid();

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

                ChangeState(LinkConnectionState.Opening);
                _loopTask = Task.Run(async () => await Loop().ConfigureAwait(false), _disposeCancellation);
            }
        }

        public Task<IModel> CreateModelAsync(CancellationToken cancellation)
        {
            return _storage.PutAsync(conn => conn.CreateModel(), cancellation);
        }

        public LinkConfiguration Configuration => _configuration;

        #endregion


        protected override void OnStateChange(LinkConnectionState newState)
        {
            _logger.Debug($"State change {State} -> {newState}");

            try
            {
                _configuration.StateHandler(State, newState);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Exception in state handler: {ex}");
            }
            
            base.OnStateChange(newState);
        }

        private async Task Loop()
        {
            var newState = LinkConnectionState.Opening;

            while (true)
            {
                if (_disposeCancellation.IsCancellationRequested)
                {
                    newState = LinkConnectionState.Stopping;
                }

                ChangeState(newState);

                try
                {
                    switch (State)
                    {
                        case LinkConnectionState.Opening:
                        case LinkConnectionState.Reopening:
                            newState = await OpenReopenAsync(State == LinkConnectionState.Reopening)
                                .ConfigureAwait(false)
                                ? LinkConnectionState.Active
                                : LinkConnectionState.Stopping;
                            break;
                        case LinkConnectionState.Active:
                            await AsyncHelper.RunAsync(Active)
                                .ConfigureAwait(false);
                            newState = LinkConnectionState.Stopping;
                            break;
                        case LinkConnectionState.Stopping:
                            await AsyncHelper.RunAsync(Stop)
                                .ConfigureAwait(false);
                            if (_disposeCancellation.IsCancellationRequested)
                            {
                                return;
                            }
                            newState = LinkConnectionState.Reopening;
                            break;
                        default:
                            throw new NotImplementedException($"Handler for state ${State} not implemeted");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unhandled exception: {ex}");
                }
            }
        }

        private async Task<bool> OpenReopenAsync(bool reopen)
        {
            using (var yieldCts = new CancellationTokenSource())
            {
                var connectTask = ConnectAsync(reopen, yieldCts);

                try
                {
                    await _storage.YieldAsync(yieldCts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // No Op
                }

                return await connectTask
                    .ConfigureAwait(false);
            }
        }

        private async Task<bool> ConnectAsync(bool reopen, CancellationTokenSource cts)
        {
            try
            {
                if (_disposeCancellation.IsCancellationRequested)
                    return false;

                if (reopen)
                {
                    var timeout = _configuration.RecoveryInterval;
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
                if (await AsyncHelper.RunAsync(Connect)
                    .ConfigureAwait(false))
                {
                    return true;
                }

                return false;
            }
            finally
            {
                cts.Cancel();
            }
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

        private void Stop()
        {
            _connectionActiveCts?.Cancel();
            _connectionActiveCts?.Dispose();
            _connectionActiveCts = null;

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

        }

        private void Active()
        {
            Connected?.Invoke(this, EventArgs.Empty);

            using (var cts =
                CancellationTokenSource.CreateLinkedTokenSource(_disposeCancellation, _connectionActiveCts.Token))
            {
                try
                {
                    while (true)
                    {
                        ActionItem<IConnection> item;

                        try
                        {
                            item = _storage.Wait(cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        try
                        {
                            item.TrySetResult(item.Value(_connection));
                        }
                        catch (Exception ex)
                        {
                            _storage.PutRetry(new[] { item }, CancellationToken.None);
                            _logger.Error($"Cannot create model: {ex.Message}");
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Processing stopped: {ex}");
                }
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
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

            // if initialized by application, exit
            if (e.Initiator == ShutdownInitiator.Application) return;

            _connectionActiveCts?.Cancel();
        }
    }
}