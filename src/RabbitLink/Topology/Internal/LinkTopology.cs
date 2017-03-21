#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Configuration;
using RabbitLink.Connection;
using RabbitLink.Internals;
using RabbitLink.Logging;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkTopology : ILinkTopology
    {
        #region .ctor

        public LinkTopology(LinkConfiguration configuration, ILinkChannel channel, ILinkTopologyHandler handler,
            bool once)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _configuration = configuration;
            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            _handler = handler;
            _isOnce = once;

            _disposedCancellationSource = new CancellationTokenSource();
            _disposedCancellation = _disposedCancellationSource.Token;

            Channel = channel;
            Channel.Disposed += ChannelOnDisposed;
            Channel.Ready += ChannelOnReady;

            _logger.Debug($"Created(channelId: {Channel.Id}, once: {once})");

            ScheduleConfiguration(false);
        }

        #endregion

        #region Events

        public event EventHandler Disposed;

        #endregion

        #region Schedule configuration

        public void ScheduleConfiguration(bool delay)
        {
            if (!Channel.IsOpen || _disposedCancellation.IsCancellationRequested)
            {
                return;
            }

            if (_isOnce && Configured)
                return;

            Configured = false;

            try
            {
                _eventLoop.ScheduleAsync(async () =>
                {
                    if (delay)
                    {
                        _logger.Info($"Retrying in {_configuration.TopologyRecoveryInterval.TotalSeconds:0.###}s");

                        await Task.Delay(_configuration.TopologyRecoveryInterval, _disposedCancellation)
                            .ConfigureAwait(false);
                    }

                    await Task.Run(async () =>
                    {
                        await ConfigureAsync()
                            .ConfigureAwait(false);
                    }, _disposedCancellation)
                        .ConfigureAwait(false);
                }, _disposedCancellation)
                    .ConfigureAwait(false);
            }
            catch
            {
                // no op
            }
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (_disposedCancellationSource.IsCancellationRequested) return;

            _logger.Debug("Disposing");
            _disposedCancellationSource.Cancel();
            _disposedCancellationSource.Dispose();
            _eventLoop.Dispose();

            Channel.Ready -= ChannelOnReady;
            Channel.Disposed -= ChannelOnDisposed;

            Channel.Dispose();

            _logger.Debug("Disposed");
            _logger.Dispose();

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Configure

        private async Task ConfigureAsync()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            if (!Channel.IsOpen)
                return;

            if (Configured && _isOnce)
                return;

            _logger.Info("Configuring topology");
            try
            {
                await _handler.Configure(new LinkTopologyConfig(_logger, Channel))
                    .ConfigureAwait(false);
            }           
            catch (Exception ex)
            {
                _logger.Warning("Exception on configuration: {0}", ex);
                try
                {
                    await _handler.ConfigurationError(ex)
                        .ConfigureAwait(false);
                }
                catch (Exception handlerException)
                {
                    _logger.Error("Error in error handler: {0}", handlerException);
                }

                ScheduleConfiguration(true);
                return;
            }

            Configured = true;

            try
            {
                await _handler.Ready()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error("Error in ready handler: {0}", ex);
            }

            _logger.Info("Topology configured");

            if (_isOnce)
            {
                _logger.Info("Once topology configured, disposing");
#pragma warning disable 4014
                // ReSharper disable once MethodSupportsCancellation
                Task.Run(() => Dispose());
#pragma warning restore 4014
            }
        }

        #endregion

        #region Fields

        private readonly CancellationTokenSource _disposedCancellationSource;
        private readonly CancellationToken _disposedCancellation;
        private readonly EventLoop _eventLoop = new EventLoop();
        private readonly ILinkTopologyHandler _handler;
        private readonly bool _isOnce;
        private readonly ILinkLogger _logger;
        private readonly LinkConfiguration _configuration;

        #endregion

        #region Properties

        public Guid Id { get; } = Guid.NewGuid();
        public bool Configured { get; private set; }
        public ILinkChannel Channel { get; }

        #endregion

        #region Channel Event Handlers

        private void ChannelOnReady(object sender, EventArgs eventArgs)
        {
            if (!_disposedCancellation.IsCancellationRequested)
            {
                if (_isOnce && Configured)
                    return;

#pragma warning disable 4014
                ScheduleConfiguration(false);
#pragma warning restore 4014
            }
        }

        private void ChannelOnDisposed(object sender, EventArgs eventArgs)
        {
            _logger.Debug("Channel disposed, disposing...");
            Dispose();
        }

        #endregion
    }
}