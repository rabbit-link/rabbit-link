#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;

#endregion

namespace RabbitLink
{
    public static class LinkExtensions
    {
        #region Base

        public static IDisposable CreateTopologyConfigurator(this Link @this,
            Func<ILinkTopologyConfig, Task> configure, Func<Task> ready = null,
            Func<Exception, Task> configurationError = null)
        {
            if (ready == null)
            {
                ready = () => Task.FromResult((object)null);
            }

            if (configurationError == null)
            {
                configurationError = ex => Task.FromResult((object)null);
            }

            return @this.CreateTopologyConfigurator(new LinkActionsTopologyHandler(configure, ready, configurationError));
        }

        public static IDisposable CreatePersistentTopologyConfigurator(this Link @this,
            Func<ILinkTopologyConfig, Task> configure, Func<Task> ready = null,
            Func<Exception, Task> configurationError = null)
        {
            if (ready == null)
            {
                ready = () => Task.FromResult((object)null);
            }

            if (configurationError == null)
            {
                configurationError = ex => Task.FromResult((object)null);
            }

            return
                @this.CreatePersistentTopologyConfigurator(new LinkActionsTopologyHandler(configure, ready,
                    configurationError));
        }        

        #endregion

        #region Async

        public static async Task ConfigureTopologyAsync(this Link @this,
            Func<ILinkTopologyConfig, Task> configure, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource();
            var configurator = @this.CreateTopologyConfigurator(configure, () =>
            {
                completion.TrySetResult();
                return Task.FromResult((object)null);
            }, ex =>
            {
                completion.TrySetException(ex);
                return Task.FromResult((object)null);
            });

            using (cancellationToken.Register(() =>
            {
                configurator.Dispose();
                completion.TrySetCanceled();
            }))
            {
                await completion.Task
                    .ConfigureAwait(false);
            }
        }

        public static async Task ConfigureTopologyAsync(this Link @this,
            Func<ILinkTopologyConfig, Task> configure, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                await @this.ConfigureTopologyAsync(configure, cts.Token)
                    .ConfigureAwait(false);
            }            
        }

        public static Task ConfigureTopologyAsync(this Link @this, Func<ILinkTopologyConfig, Task> configure)
        {
            return @this.ConfigureTopologyAsync(configure, CancellationToken.None);
        }

        #endregion

        #region Sync

        public static void ConfigureTopology(this Link @this,
            Func<ILinkTopologyConfig, Task> configure, TimeSpan timeout)
        {
            @this.ConfigureTopologyAsync(configure, timeout)
                .WaitAndUnwrapException();
        }

        public static void ConfigureTopology(this Link @this,
            Func<ILinkTopologyConfig, Task> configure)
        {
            @this.ConfigureTopologyAsync(configure)
                .WaitAndUnwrapException();
        }

        #endregion
    }
}