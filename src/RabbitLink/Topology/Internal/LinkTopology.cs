#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Connection;
using RabbitLink.Internals.Async;
using RabbitLink.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkTopology : ILinkTopology, ILinkChannelHandler
    {
        #region Fields

        private readonly ILinkChannel _channel;
        private readonly ILinkTopologyHandler _handler;
        private readonly bool _isOnce;
        private readonly ILinkLogger _logger;
        private readonly object _sync = new object();
        private readonly LinkTopologyRunner<object> _topologyRunner;

        #endregion

        #region Ctor

        public LinkTopology(ILinkChannel channel, ILinkTopologyHandler handler, bool once)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _isOnce = once;

            _channel.Disposed += ChannelOnDisposed;

            _topologyRunner = new LinkTopologyRunner<object>(_logger, async cfg =>
            {
                await _handler.Configure(cfg)
                    .ConfigureAwait(false);
                return null;
            });

            _logger = _channel.Connection.Configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})")
                      ?? throw new InvalidOperationException("Cannot create logger");

            _logger.Debug($"Created(channelId: {_channel.Id}, once: {once})");

            _channel.Initialize(this);
        }

        #endregion

        #region ILinkChannelHandler Members

        public async Task OnActive(IModel model, CancellationToken cancellation)
        {
            var newState = State;

            while (true)
            {
                if (cancellation.IsCancellationRequested)
                {
                    newState = LinkTopologyState.Stop;
                }

                if (newState != State)
                {
                    _logger.Debug($"State change {State} -> {newState}");
                    State = newState;
                }

                switch (State)
                {
                    case LinkTopologyState.Init:
                        newState = LinkTopologyState.Configure;
                        break;
                    case LinkTopologyState.Configure:
                    case LinkTopologyState.Reconfigure:
                        newState = await OnConfigureAsync(model, State == LinkTopologyState.Reconfigure,
                                cancellation)
                            .ConfigureAwait(false);
                        break;
                    case LinkTopologyState.Ready:
                        if (_isOnce)
                        {
                            _logger.Info("Once topology configured, going to dispose");
                            newState = LinkTopologyState.Dispose;
                        }
                        else
                        {
                            await cancellation.WaitCancellation()
                                .ConfigureAwait(false);
                            newState = LinkTopologyState.Configure;
                        }
                        break;
                    case LinkTopologyState.Dispose:
#pragma warning disable 4014
                        Task.Factory.StartNew(Dispose, TaskCreationOptions.LongRunning);
#pragma warning restore 4014
                        return;
                    case LinkTopologyState.Stop:
                        return;
                    default:
                        throw new NotImplementedException($"Handler for state ${State} not implemeted");
                }
            }
        }

        public Task OnConnecting(CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        public void MessageAck(BasicAckEventArgs info)
        {
        }

        public void MessageNack(BasicNackEventArgs info)
        {
        }

        public void MessageReturn(BasicReturnEventArgs info)
        {
        }

        #endregion

        #region ILinkTopology Members

        public LinkTopologyState State { get; private set; } = LinkTopologyState.Init;

        public event EventHandler Disposed;


        public void Dispose()
        {
            Dispose(false);
        }


        public Guid Id { get; } = Guid.NewGuid();

        #endregion

        private async Task<LinkTopologyState> OnConfigureAsync(IModel model, bool retry, CancellationToken cancellation)
        {
            if (retry)
            {
                try
                {
                    _logger.Info($"Retrying in {_configuration.TopologyRecoveryInterval.TotalSeconds:0.###}s");
                    await Task.Delay(_configuration.TopologyRecoveryInterval, cancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return LinkTopologyState.Reconfigure;
                }
            }

            _logger.Info("Configuring topology");

            try
            {
                await _topologyRunner
                    .RunAsync(model, cancellation)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Exception on configuration: {ex}");

                try
                {
                    await _handler.ConfigurationError(ex)
                        .ConfigureAwait(false);
                }
                catch (Exception handlerException)
                {
                    _logger.Error($"Error in error handler: {handlerException}");
                }

                return LinkTopologyState.Reconfigure;
            }

            try
            {
                await _handler.Ready()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ready handler: {ex}");
            }

            _logger.Info("Topology configured");

            return LinkTopologyState.Ready;
        }

        private void Dispose(bool byChannel)
        {
            if (State == LinkTopologyState.Dispose)
                return;

            lock (_sync)
            {
                if (State == LinkTopologyState.Dispose)
                    return;

                _logger.Debug("Disposing");

                _channel.Disposed -= ChannelOnDisposed;
                if (!byChannel)
                {
                    _channel.Dispose();
                }

                State = LinkTopologyState.Dispose;

                _logger.Debug("Disposed");
                _logger.Dispose();

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }


        private void ChannelOnDisposed(object sender, EventArgs eventArgs)
        {
            _logger.Debug("Channel disposed, disposing...");
            Dispose(true);
        }
    }
}