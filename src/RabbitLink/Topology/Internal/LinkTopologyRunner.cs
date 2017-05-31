#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Queues;
using RabbitLink.Logging;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkTopologyRunner<T>
    {
        #region Fields

        private readonly Func<ILinkTopologyConfig, Task<T>> _configureFunc;
        private readonly ILinkLogger _logger;
        private readonly bool _useThreads;

        #endregion

        #region Ctor

        public LinkTopologyRunner(ILinkLogger logger, bool useThreads, Func<ILinkTopologyConfig, Task<T>> configureFunc)
        {
            _configureFunc = configureFunc;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _useThreads = useThreads;
        }

        #endregion

        public async Task<T> RunAsync(IModel model, CancellationToken cancellation)
        {
            var queue = new ActionQueue<IModel>();
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

            return await configTask
                .ConfigureAwait(false);
        }

        private async Task StartQueueWorker(IModel model, ActionQueue<IModel> queue,
            CancellationToken cancellation)
        {
            if (_useThreads)
            {
                await Task.Factory.StartNew(() =>
                    {
                        while (true)
                        {
                            var item = queue.Wait(cancellation);
                            try
                            {
                                var result = item.Value(model);
                                item.TrySetResult(result);
                            }
                            catch (Exception ex)
                            {
                                item.TrySetException(ex);
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
                                var result = item.Value(model);
                                item.TrySetResult(result);
                            }
                            catch (Exception ex)
                            {
                                item.TrySetException(ex);
                            }
                        }, cancellation)
                        .ConfigureAwait(false);
                }
            }
        }

        private Task<T> RunConfiguration(ActionQueue<IModel> queue, CancellationToken cancellation)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var invoker = new ActionQueueInvoker<IModel>(queue, cancellation);
                    var config = new LinkTopologyConfig(_logger, invoker);
                    return await _configureFunc(config)
                        .ConfigureAwait(false);
                }
                finally
                {
                    queue.Complete();
                }
            }, cancellation);
        }
    }
}