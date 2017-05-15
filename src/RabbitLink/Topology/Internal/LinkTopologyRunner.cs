using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Queues;
using RabbitLink.Logging;
using RabbitMQ.Client;

namespace RabbitLink.Topology.Internal
{
    internal class LinkTopologyRunner
    {
        private readonly ILinkTopologyHandler _handler;
        private readonly ILinkLogger _logger;
        private readonly Func<ILinkTopologyConfig, Task> _configureFunc;

        public LinkTopologyRunner(ILinkLogger logger, Func<ILinkTopologyConfig, Task> configureFunc)
        {
            _configureFunc = configureFunc;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(IModel model, CancellationToken cancellation)
        {
            var queue = new ConcurrentWorkQueue<Action<IModel>, object>();

            Task Invoke(Action<IModel> action) => queue.PutAsync(action, cancellation);

            var config = new LinkTopologyConfig(_logger, Invoke);

            var configTask = Task.Run(async () =>
            {
                try
                {
                    await _configureFunc(config)
                        .ConfigureAwait(false);
                }
                finally
                {
                    queue.CompleteAdding();
                }
            }, cancellation);

            try
            {
                while (true)
                {
                    var item = await queue.WaitAsync(cancellation)
                        .ConfigureAwait(false);

                    
                }
            }
            catch
            {
                // No Op
            }
        }
    }
}
