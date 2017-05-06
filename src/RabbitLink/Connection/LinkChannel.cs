#region Usings

using System;
using System.IO;
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
    internal class LinkChannel : ILinkChannel
    {
        #region Fields

        private readonly LinkConfiguration _configuration;
        private readonly CancellationToken _disposedCancellation;
        private readonly ILinkConnection _connection;
        private readonly ILinkLogger _logger;

        private readonly CancellationTokenSource _disposeCts;

        private readonly object _sync = new object();

        private IModel _model;
        private ILinkChannelHandler _handler;

        private Task _loopTask;
        private CancellationTokenSource _modelActiveCts;

        #endregion

        #region Ctor

        public LinkChannel(LinkConfiguration configuration, ILinkConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            _disposeCts = new CancellationTokenSource();
            _disposedCancellation = _disposeCts.Token;

            _connection.Disposed += ConnectionOnDisposed;

            _logger.Debug($"Created(connectionId: {_connection.Id:D})");
        }

        #endregion

        #region ILinkChannel Members

        public void Dispose()
        {
            if (State == LinkChannelState.Disposed)
                return;

            lock (_sync)
            {
                if (State == LinkChannelState.Disposed)
                    return;

                _disposeCts.Cancel();
                _disposeCts.Dispose();

                _logger.Debug("Disposing");

                try
                {
                    _loopTask?.Wait(CancellationToken.None);
                }
                catch
                {
                    // no op
                }

                _connection.Disposed -= ConnectionOnDisposed;
                State = LinkChannelState.Disposed;

                Disposed?.Invoke(this, EventArgs.Empty);

                _logger.Debug("Disposed");
                _logger.Dispose();
            }
        }

        public Guid Id { get; } = Guid.NewGuid();

        public LinkChannelState State { get; private set; }

        public event EventHandler Disposed;

        public void Initialize(ILinkChannelHandler handler)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (State != LinkChannelState.Init)
                throw new InvalidOperationException("Already initialized");

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                if (State != LinkChannelState.Init)
                    throw new InvalidOperationException("Already initialized");

                _handler = handler;
                State = LinkChannelState.Open;
                _loopTask = Task.Run(async () => await Loop().ConfigureAwait(false), _disposedCancellation);
            }
        }

        #endregion

        #region Loop

        private async Task Loop()
        {
            var newState = State;

            while (true)
            {
                if (_disposedCancellation.IsCancellationRequested)
                {
                    newState = LinkChannelState.Stop;
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
                        case LinkChannelState.Open:
                        case LinkChannelState.Reopen:
                            newState = await OnOpenReopenAsync(State == LinkChannelState.Reopen)
                                .ConfigureAwait(false);
                            break;
                        case LinkChannelState.Active:
                            newState = await OnActive()
                                .ConfigureAwait(false);
                            break;
                        case LinkChannelState.Stop:
                            newState = await OnStop()
                                .ConfigureAwait(false);
                            break;
                        case LinkChannelState.Disposed:
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

        private async Task<LinkChannelState> OnOpenReopenAsync(bool reopen)
        {
            _modelActiveCts = new CancellationTokenSource();
            using (var openCts = new CancellationTokenSource())
            {
                var openCancellation = openCts.Token;
                var openTask = Task.Run(
                    async () => await _handler.OnConnecting(openCancellation).ConfigureAwait(false),
                    openCancellation
                );

                try
                {
                    if (reopen && _connection.IsConnected)
                    {
                        _logger.Info($"Reopening in {_configuration.ChannelRecoveryInterval.TotalSeconds:0.###}s");
                        _model = await _connection
                            .CreateModelWaitAsync(_configuration.ChannelRecoveryInterval, _disposedCancellation)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.Info(reopen ? "Reopening" : "Opening");
                        _model = await _connection
                            .CreateModelWaitAsync(TimeSpan.Zero, _disposedCancellation)
                            .ConfigureAwait(false);
                    }

                    _model.ModelShutdown += ModelOnModelShutdown;
                    _model.CallbackException += ModelOnCallbackException;
                    _model.BasicAcks += ModelOnBasicAcks;
                    _model.BasicNacks += ModelOnBasicNacks;
                    _model.BasicReturn += ModelOnBasicReturn;

                    _logger.Debug($"Model created, channel number: {_model.ChannelNumber}");
                }
                catch (Exception ex)
                {
                    _logger.Error("Cannot create model: {0}", ex.Message);
                    return LinkChannelState.Stop;
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
            return LinkChannelState.Active;
        }

        private Task<LinkChannelState> OnStop()
        {
            _modelActiveCts?.Cancel();
            _modelActiveCts?.Dispose();

            try
            {
                _model?.Dispose();
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                _logger.Warning("Model cleaning exception: {0}", ex);
            }

            return Task.FromResult(_disposedCancellation.IsCancellationRequested
                ? LinkChannelState.Disposed
                : LinkChannelState.Reopen
            );
        }

        private async Task<LinkChannelState> OnActive()
        {
            using (var activeCts =
                CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, _modelActiveCts.Token))
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

            return LinkChannelState.Stop;
        }

        #endregion

        #endregion

        #region Event handlers

        private void ConnectionOnDisposed(object sender, EventArgs eventArgs)
        {
            _logger.Debug("Connection disposed, disposing...");
            Dispose();
        }

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
        {
            _logger.Error($"Callback exception: {e.Exception}");
        }

        private void ModelOnModelShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.Info($"Shutdown, Initiator: {e.Initiator}, Code: {e.ReplyCode}, Message: {e.ReplyText}");

            if (e.Initiator == ShutdownInitiator.Application) return;

            _modelActiveCts?.Cancel();
        }

        #endregion
    }
}