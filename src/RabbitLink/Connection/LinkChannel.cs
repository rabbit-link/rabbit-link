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

        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly ILinkLogger _logger;
        private readonly AsyncLock _sync = new AsyncLock();

        private IModel _model;

        #endregion

        #region Ctor

        public LinkChannel(LinkConfiguration configuration, ILinkConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            _disposedCancellationSource = new CancellationTokenSource();
            _disposedCancellation = _disposedCancellationSource.Token;

            Connection.Disposed += ConnectionOnDisposed;

            _logger.Debug($"Created(connectionId: {Connection.Id:D})");

            ScheduleReopen(false);
        }

        #endregion

        #region ILinkChannel Members

        public void Dispose()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            using(_sync.Lock(CancellationToken.None))
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _disposedCancellationSource.Cancel();
                _disposedCancellationSource.Dispose();

                _logger.Debug("Disposing");

                Cleanup();

                Connection.Disposed -= ConnectionOnDisposed;

                Disposed?.Invoke(this, EventArgs.Empty);

                _logger.Debug("Disposed");
                _logger.Dispose();
            }
        }

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsOpen =>
            !_disposedCancellation.IsCancellationRequested &&
            Connection.IsConnected &&
            _model?.IsOpen == true;

        public ILinkConnection Connection { get; }

        public event EventHandler Ready;
        public event EventHandler<ShutdownEventArgs> Shutdown;
        public event EventHandler<BasicAckEventArgs> Ack;
        public event EventHandler<BasicNackEventArgs> Nack;
        public event EventHandler<BasicReturnEventArgs> Return;
        public event EventHandler Disposed;

        public async Task InvokeActionAsync(Action<IModel> action, CancellationToken cancellation)
        {
            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            using (var compositeCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(_disposedCancellation, cancellation))
            {
                try
                {
                    using (await _sync.LockAsync(compositeCancellation.Token).ConfigureAwait(false))
                    {
                        if (_model?.IsOpen != true)
                            throw new InvalidOperationException("Channel closed");

                        action(_model);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (_disposedCancellation.IsCancellationRequested)
                    {
                        throw new ObjectDisposedException(GetType().Name);
                    }

                    throw;
                }
            }
        }

        public Task InvokeActionAsync(Action<IModel> action)
        {
            return InvokeActionAsync(action, CancellationToken.None);
        }

        #endregion

        private void ConnectionOnDisposed(object sender, EventArgs eventArgs)
        {
            _logger.Debug("Connection disposed, disposing...");
            Dispose();
        }

        private async Task OpenAsync(bool delay)
        {
            if (IsOpen || _disposedCancellation.IsCancellationRequested)
                return;

            using (await _sync.LockAsync(_disposedCancellation).ConfigureAwait(false))
            {
                if (IsOpen || _disposedCancellation.IsCancellationRequested)
                    return;

                Cleanup();

                try
                {
                    if (delay && Connection.IsConnected)
                    {
                        _logger.Info($"Opening in {_configuration.ChannelRecoveryInterval.TotalSeconds:0.###}s");
                        _model = await Connection
                            .CreateModelWaitAsync(_configuration.ChannelRecoveryInterval, _disposedCancellation)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.Info("Opening");
                        _model = await Connection
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
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Error("Cannot create model: {0}", ex.Message);
                    ScheduleReopen(true);
                    return;
                }

                Ready?.Invoke(this, EventArgs.Empty);

                _logger.Info($"Opened(channelNumber: {_model.ChannelNumber})");
            }
        }

        private void ScheduleReopen(bool delay)
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            Task.Run(async () => await OpenAsync(delay).ConfigureAwait(false), _disposedCancellation);
        }

        private void Cleanup()
        {
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
        }

        private void ModelOnBasicReturn(object sender, BasicReturnEventArgs e)
        {
            _logger.Debug(
                $"Return, code: {e.ReplyCode}, message: {e.ReplyText},  message id:{e.BasicProperties.MessageId}");
            Return?.Invoke(this, e);
        }

        private void ModelOnBasicNacks(object sender, BasicNackEventArgs e)
        {
            _logger.Debug($"Nack, tag: {e.DeliveryTag}, multiple: {e.Multiple}");
            Nack?.Invoke(this, e);
        }

        private void ModelOnBasicAcks(object sender, BasicAckEventArgs e)
        {
            _logger.Debug($"Ack, tag: {e.DeliveryTag}, multiple: {e.Multiple}");
            Ack?.Invoke(this, e);
        }

        private void ModelOnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            _logger.Error($"Callback exception: {e.Exception}");
        }

        private void ModelOnModelShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.Info($"Shutdown, Initiator: {e.Initiator}, Code: {e.ReplyCode}, Message: {e.ReplyText}");

            if (e.Initiator == ShutdownInitiator.Application) return;

            Shutdown?.Invoke(this, e);

            ScheduleReopen(true);
        }
    }
}