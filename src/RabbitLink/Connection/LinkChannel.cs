#region Usings

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals;
using RabbitLink.Internals.Async;
using RabbitLink.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Connection
{
    internal class LinkChannel : AsyncStateMachine<LinkChannelState>, ILinkChannel
    {
        #region Fields

        private readonly ILinkConnection _connection;
        private readonly ILinkLogger _logger;
        private readonly TimeSpan _recoveryInterval;
        private readonly LinkStateHandler<LinkChannelState> _stateHandler;

        private readonly CancellationTokenSource _disposeCts;
        private readonly CancellationToken _disposeCancellation;

        private readonly object _sync = new object();

        private IModel _model;
        private ILinkChannelHandler _handler;

        private Task _loopTask;
        private CancellationTokenSource _modelActiveCts;

        #endregion

        #region Ctor

        public LinkChannel(ILinkConnection connection, LinkStateHandler<LinkChannelState>stateHandler,
            TimeSpan recoveryInterval)
            : base(LinkChannelState.Init)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger =
                connection.Configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})")
                ?? throw new ArgumentException("Cannot create logger", nameof(connection.Configuration.LoggerFactory));

            if (recoveryInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(recoveryInterval), "Must be greater than zero");

            _stateHandler = stateHandler ?? throw new ArgumentNullException(nameof(stateHandler));

            _recoveryInterval = recoveryInterval;

            _disposeCts = new CancellationTokenSource();
            _disposeCancellation = _disposeCts.Token;

            _connection.Disposed += ConnectionOnDisposed;

            _logger.Debug($"Created(connectionId: {_connection.Id:D})");
        }

        #endregion

        #region ILinkChannel Members

        private void Dispose(bool byConnection)
        {
            if (State == LinkChannelState.Disposed)
                return;

            lock (_sync)
            {
                if (State == LinkChannelState.Disposed)
                    return;

                _logger.Debug($"Disposing ( by connection: {byConnection} )");

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

                _connection.Disposed -= ConnectionOnDisposed;
                ChangeState(LinkChannelState.Disposed);

                Disposed?.Invoke(this, EventArgs.Empty);

                _logger.Debug("Disposed");
                _logger.Dispose();
            }
        }

        public void Dispose()
            => Dispose(false);

        public Guid Id { get; } = Guid.NewGuid();

        public event EventHandler Disposed;

        public void Initialize(ILinkChannelHandler handler)
        {
            if (_disposeCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (State != LinkChannelState.Init)
                throw new InvalidOperationException("Already initialized");

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_sync)
            {
                if (_disposeCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (State != LinkChannelState.Init)
                    throw new InvalidOperationException("Already initialized");

                _handler = handler;
                ChangeState(LinkChannelState.Opening);
                _loopTask = Task.Run(async () => await Loop().ConfigureAwait(false), _disposeCancellation);
            }
        }

        public ILinkConnection Connection => _connection;

        #endregion

        protected override void OnStateChange(LinkChannelState newState)
        {
            _logger.Debug($"State change {State} -> {newState}");

            try
            {
                _stateHandler(State, newState);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Exception in state handler: {ex}");
            }

            base.OnStateChange(newState);
        }

        #region Loop

        private async Task Loop()
        {
            var newState = LinkChannelState.Opening;

            while (true)
            {
                if (_disposeCancellation.IsCancellationRequested)
                {
                    newState = LinkChannelState.Stopping;
                }

                ChangeState(newState);

                try
                {
                    switch (State)
                    {
                        case LinkChannelState.Opening:
                        case LinkChannelState.Reopening:
                            newState = await OpenReopenAsync(State == LinkChannelState.Reopening)
                                .ConfigureAwait(false)
                                ? LinkChannelState.Active
                                : LinkChannelState.Stopping;
                            break;
                        case LinkChannelState.Active:
                            await ActiveAsync()
                                .ConfigureAwait(false);
                            newState = LinkChannelState.Stopping;
                            break;
                        case LinkChannelState.Stopping:
                            await AsyncHelper.RunAsync(Stop)
                                .ConfigureAwait(false);
                            if (_disposeCancellation.IsCancellationRequested)
                            {
                                return;
                            }
                            newState = LinkChannelState.Reopening;
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

        #region Actions

        private async Task<bool> OpenReopenAsync(bool reopen)
        {
            using (var openCts = new CancellationTokenSource())
            {
                var openCancellation = openCts.Token;
                var openTask = Task.Run(
                    async () => await _handler.OnConnecting(openCancellation).ConfigureAwait(false),
                    CancellationToken.None
                );

                try
                {
                    if (reopen && _connection.State == LinkConnectionState.Active)
                    {
                        _logger.Info($"Reopening in {_recoveryInterval.TotalSeconds:0.###}s");
                        await Task.Delay(_recoveryInterval, _disposeCancellation)
                            .ConfigureAwait(false);
                    }

                    _logger.Info("Opening");
                    _model = await _connection
                        .CreateModelAsync(_disposeCancellation)
                        .ConfigureAwait(false);

                    _modelActiveCts = new CancellationTokenSource();

                    _model.ModelShutdown += ModelOnModelShutdown;
                    _model.CallbackException += ModelOnCallbackException;
                    _model.BasicAcks += ModelOnBasicAcks;
                    _model.BasicNacks += ModelOnBasicNacks;
                    _model.BasicReturn += ModelOnBasicReturn;

                    _logger.Debug($"Model created, channel number: {_model.ChannelNumber}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Cannot create model: {ex.Message}");
                    return false;
                }
                finally
                {
                    openCts.Cancel();

                    try
                    {
                        await openTask
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Connecting handler throws exception: {ex}");
                    }
                }
            }

            _logger.Info($"Opened(channelNumber: {_model.ChannelNumber})");
            return true;
        }

        private void Stop()
        {
            _modelActiveCts?.Cancel();
            _modelActiveCts?.Dispose();
            _modelActiveCts = null;

            try
            {
                _model?.Dispose();
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                _logger.Warning($"Model cleaning exception: {ex}");
            }
        }

        private async Task ActiveAsync()
        {
            using (var activeCts =
                CancellationTokenSource.CreateLinkedTokenSource(_disposeCancellation, _modelActiveCts.Token))
            {
                try
                {
                    await _handler.OnActive(_model, activeCts.Token)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Processing handler exception: {ex}");
                }

                await activeCts.Token.WaitCancellation()
                    .ConfigureAwait(false);
            }
        }

        #endregion

        #endregion

        #region Event handlers

        private void ConnectionOnDisposed(object sender, EventArgs eventArgs)
            => Dispose(true);


        private void ModelOnBasicReturn(object sender, BasicReturnEventArgs e)
        {
            _logger.Debug(
                $"Return, code: {e.ReplyCode}, message: {e.ReplyText},  message id:{e.BasicProperties.MessageId}");

            _handler.MessageReturn(e);
        }

        private void ModelOnBasicNacks(object sender, BasicNackEventArgs e)
        {
            _logger.Debug($"Nack, tag: {e.DeliveryTag}, multiple: {e.Multiple}");
            _handler.MessageNack(e);
        }

        private void ModelOnBasicAcks(object sender, BasicAckEventArgs e)
        {
            _logger.Debug($"Ack, tag: {e.DeliveryTag}, multiple: {e.Multiple}");
            _handler.MessageAck(e);
        }

        private void ModelOnCallbackException(object sender, CallbackExceptionEventArgs e)
            => _logger.Error($"Callback exception: {e.Exception}");


        private void ModelOnModelShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.Info($"Shutdown, Initiator: {e.Initiator}, Code: {e.ReplyCode}, Message: {e.ReplyText}");

            if (e.Initiator == ShutdownInitiator.Application) return;

            _modelActiveCts?.Cancel();
        }

        #endregion
    }
}
