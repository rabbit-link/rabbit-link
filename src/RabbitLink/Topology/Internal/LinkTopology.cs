#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Async;
using RabbitLink.Configuration;
using RabbitLink.Connection;
using RabbitLink.Internals.Queues;
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
        private readonly LinkConfiguration _configuration;
        private readonly ILinkTopologyHandler _handler;
        private readonly bool _isOnce;
        private readonly ILinkLogger _logger;
        private readonly object _sync = new object();

        #endregion

        #region Ctor

        public LinkTopology(LinkConfiguration configuration, ILinkChannel channel, ILinkTopologyHandler handler,
            bool once)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));

            _logger = _configuration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(configuration.LoggerFactory));

            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _isOnce = once;

            _channel.Disposed += ChannelOnDisposed;

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

                try
                {
                    switch (State)
                    {
                        case LinkTopologyState.Init:
                            newState = LinkTopologyState.Configure;
                            break;
                        case LinkTopologyState.Configure:
                        case LinkTopologyState.Reconfigure:
                            newState = await OnConfigureAsync(model, State == LinkTopologyState.Reconfigure, cancellation)
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
                        default:
                            return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unhandled exception: {ex}");
                }
            }
        }

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

            var queue = new ConcurrentWorkQueue<Action<IModel>, object>();

            _logger.Info("Configuring topology");
            var configTask = RunConfiguration(queue, cancellation);

            try
            {
                await StartQueueWorker(model, queue, cancellation)
                    .ConfigureAwait(false);
            }
            catch
            {
                // No Op
            }

            try
            {
                await configTask
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

        private async Task StartQueueWorker(IModel model, ConcurrentWorkQueue<Action<IModel>, object> queue,
            CancellationToken cancellation)
        {
            if (_configuration.UseThreads)
            {
                await Task.Factory.StartNew(() =>
                    {
                        while (true)
                        {
                            var item = queue.Wait(cancellation);
                            try
                            {
                                item.Value(model);
                                item.Completion.TrySetResult(null);
                            }
                            catch (Exception ex)
                            {
                                item.Completion.TrySetException(ex);
                            }
                        }
                        // ReSharper disable once FunctionNeverReturns
                    }, cancellation, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ConfigureAwait(false);
            }
            else
            {
                while (true)
                {
                    var item = await queue.WaitAsync(cancellation)
                        .ConfigureAwait(false);

                    await Task.Factory.StartNew(() =>
                        {
                            try
                            {

                                item.Value(model);
                                item.Completion.TrySetResult(null);
                            }
                            catch (Exception ex)
                            {
                                item.Completion.TrySetException(ex);
                            }
                        }, cancellation)
                        .ConfigureAwait(false);
                }
            }
        }

        private Task RunConfiguration(ConcurrentWorkQueue<Action<IModel>, object> queue, CancellationToken cancellation)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var config = new LinkTopologyConfig(_logger, a => queue.PutAsync(a, cancellation));
                    await _handler.Configure(config)
                        .ConfigureAwait(false);
                }
                finally
                {
                    queue.CompleteAdding();
                }
            }, cancellation);
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

    public enum LinkTopologyState
    {
        Init,
        Configure,
        Reconfigure,
        Ready,
        Stop,
        Dispose
    }
}