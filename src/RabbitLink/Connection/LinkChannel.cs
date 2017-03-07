#region Usings

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Configuration;
using RabbitLink.Internals;
using RabbitLink.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Connection
{
    internal class LinkChannel : ILinkChannel
    {
        #region IDisposable

        public void Dispose()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            lock (_syncObject)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _disposedCancellationSource.Cancel();
                _disposedCancellationSource.Dispose();
            }

            _logger.Debug("Disposing");
            _eventLoop.Dispose();
            Cleanup();

            Connection.Connected -= ConnectionOnConnected;
            Connection.Disposed -= ConnectionOnDisposed;

            _logger.Debug("Disposed");
            _logger.Dispose();

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Connection callbacks

        private void ConnectionOnConnected(object sender, EventArgs eventArgs)
        {
            ScheduleReopen(false);
        }

        #endregion

        #region Fields

        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly CancellationToken _disposedCancellation;
        private readonly EventLoop _eventLoop = new EventLoop();

        private readonly LinkConfiguration _configuration;
        private readonly ILinkLogger _logger;
        private readonly object _syncObject = new object();
        private IModel _model;

        #endregion

        #region .ctor

        public LinkChannel(LinkConfiguration configuration, ILinkConnection connection)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _configuration = configuration;
            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            _disposedCancellationSource = new CancellationTokenSource();
            _disposedCancellation = _disposedCancellationSource.Token;

            Connection = connection;

            Connection.Disposed += ConnectionOnDisposed;
            Connection.Connected += ConnectionOnConnected;

            _logger.Debug($"Created(connectionId: {Connection.Id:D})");

            ScheduleReopen(false);
        }

        private void ConnectionOnDisposed(object sender, EventArgs eventArgs)
        {
            _logger.Debug("Connection disposed, disposing...");
            Dispose();
        }

        #endregion

        #region Properties

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsOpen =>
            !_disposedCancellation.IsCancellationRequested &&
            Connection.IsConnected &&
            _model?.IsOpen == true;

        public ILinkConnection Connection { get; }

        #endregion

        #region Events

        public event EventHandler Ready;
        public event EventHandler<ShutdownEventArgs> Shutdown;
        public event EventHandler<FlowControlEventArgs> FlowControl;
        public event EventHandler Recover;
        public event EventHandler<BasicAckEventArgs> Ack;
        public event EventHandler<BasicNackEventArgs> Nack;
        public event EventHandler<BasicReturnEventArgs> Return;
        public event EventHandler Disposed;

        #endregion

        #region Public methods

        public async Task InvokeActionAsync(Action<IModel> action, CancellationToken cancellation)
        {
            await Task.Delay(0)
                .ConfigureAwait(false);

            if (_disposedCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            using (var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(_disposedCancellation, cancellation))
            {
                try
                {
                    await _eventLoop.ScheduleAsync(() =>
                    {
                        if (_model?.IsOpen != true)
                            throw new InvalidOperationException("Channel closed");

                        action(_model);
                    }, compositeCancellation.Token)
                        .ConfigureAwait(false);
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

        #region State management

        private void Open()
        {
            if (!Connection.IsConnected || IsOpen || _disposedCancellation.IsCancellationRequested)
                return;

            _logger.Info("Opening");

            Cleanup();

            // Last chance to cancel
            if (!Connection.IsConnected || _disposedCancellation.IsCancellationRequested)
                return;

            try
            {
                _logger.Debug("Creating model");

                _model = Connection.CreateModel();
                _model.ModelShutdown += ModelOnModelShutdown;
                _model.CallbackException += ModelOnCallbackException;
                _model.FlowControl += ModelOnFlowControl;
                _model.BasicAcks += ModelOnBasicAcks;
                _model.BasicNacks += ModelOnBasicNacks;
                _model.BasicRecoverOk += ModelOnBasicRecoverOk;
                _model.BasicReturn += ModelOnBasicReturn;

                _logger.Debug($"Model created, channel number: {_model.ChannelNumber}");
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

        private void ScheduleReopen(bool delay)
        {
            if (!Connection.IsConnected || _disposedCancellation.IsCancellationRequested)
            {
                return;
            }

            try
            {
                _eventLoop.ScheduleAsync(async () =>
                {
                    if (delay)
                    {
                        _logger.Info($"Reopening in {_configuration.ChannelRecoveryInterval.TotalSeconds:0.###}s");
                        await Task.Delay(_configuration.ChannelRecoveryInterval, _disposedCancellation)
                            .ConfigureAwait(false);
                    }

                    Open();
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

        #endregion

        #region Model callbacks

        private void ModelOnBasicReturn(object sender, BasicReturnEventArgs e)
        {
            _logger.Debug(
                $"Return, code: {e.ReplyCode}, message: {e.ReplyText},  message id:{e.BasicProperties.MessageId}");
            Return?.Invoke(this, e);
        }

        private void ModelOnBasicRecoverOk(object sender, EventArgs e)
        {
            _logger.Debug("Recover");
            Recover?.Invoke(this, e);
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

        private void ModelOnFlowControl(object sender, FlowControlEventArgs e)
        {
            _logger.Debug($"Flow control: {e.Active}");
            FlowControl?.Invoke(this, e);
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

        #endregion
    }
}