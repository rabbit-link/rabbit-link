#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Async;
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

        #endregion

        #region Ctor

        public LinkTopologyRunner(ILinkLogger logger, Func<ILinkTopologyConfig, Task<T>> configureFunc)
        {
            _configureFunc = configureFunc;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        public async Task<T> RunAsync(IModel model, CancellationToken cancellation)
        {
            var queue = new ConcurrentActionQueue<IModel>();
            var configTask = RunConfiguration(queue, cancellation);

            await StartQueueWorker(model, queue, cancellation)
                .ConfigureAwait(false);

            return await configTask
                .ConfigureAwait(false);
        }

        private Task StartQueueWorker(IModel model, IActionQueue<IModel> queue,
            CancellationToken cancellation)
        {
            return AsyncHelper.RunAsync(() =>
            {
                while (!cancellation.IsCancellationRequested)
                {
                    ActionQueueItem<IModel> item;
                    try
                    {
                        item = queue.Wait(cancellation);
                    }
                    catch
                    {
                        break;
                    }

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
            });
        }

        private async Task<T> RunConfiguration(IActionQueue<IModel> queue, CancellationToken cancellation)
        {
            try
            {
                return await Task.Run(() =>
                    {
                        var invoker = new ActionQueueInvoker<IModel>(queue, cancellation);
                        var config = new LinkTopologyConfig(_logger, invoker);
                        return _configureFunc(config);
                    }, cancellation)
                    .ConfigureAwait(false);
            }
            finally
            {
                queue.Complete();
            }
        }
    }
}