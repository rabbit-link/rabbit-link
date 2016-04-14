#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using RabbitLink.Configuration;
using RabbitLink.Consumer;
using RabbitLink.Messaging;
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

        public static ILinkPushConsumer CreatePushConsumer<T>(this Link @this,
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Func<ILinkRecievedMessage<T>, Task> onMessage,
            bool parallel = true,
            Func<Exception, Task> configurationError = null,
            Action<Exception, ILinkRecievedMessage<byte[]>> serializationError = null,
            Action<ILinkPushConsumerConfigurationBuilder> config = null) where T : class
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            return @this.CreatePushConsumer(
                topologyConfiguration,
                cfg =>
                {
                    if (parallel)
                        cfg.AllParallelAsync(onMessage);
                    else
                        cfg.AllAsync(onMessage);
                },
                configurationError,
                serializationError,
                config
                );
        }

        public static ILinkPushConsumer CreatePushConsumer<T>(this Link @this,
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Action<ILinkRecievedMessage<T>> onMessage,
            bool parallel = true,
            Func<Exception, Task> configurationError = null,
            Action<Exception, ILinkRecievedMessage<byte[]>> serializationError = null,
            Action<ILinkPushConsumerConfigurationBuilder> config = null) where T : class
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            return @this.CreatePushConsumer<T>(
                topologyConfiguration,
                message =>
                {
                    onMessage(message);
                    return Task.FromResult((object)null);
                },
                parallel,
                configurationError,
                serializationError,
                config
                );
        }

        public static ILinkPushConsumer CreatePushConsumer(this Link @this,
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Func<ILinkRecievedMessage<object>, Task> onMessage,
            bool parallel = true,
            Func<Exception, Task> configurationError = null,
            Action<Exception, ILinkRecievedMessage<byte[]>> serializationError = null,
            Action<ILinkPushConsumerConfigurationBuilder> config = null)
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            return @this.CreatePushConsumer(
                topologyConfiguration,
                cfg =>
                {
                    if (parallel)
                        cfg.MappedParallelAsync(onMessage);
                    else
                        cfg.MappedAsync(onMessage);
                },
                configurationError,
                serializationError,
                config
                );
        }

        public static ILinkPushConsumer CreatePushConsumer(this Link @this,
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Action<ILinkRecievedMessage<object>> onMessage,
            bool parallel = true,
            Func<Exception, Task> configurationError = null,
            Action<Exception, ILinkRecievedMessage<byte[]>> serializationError = null,
            Action<ILinkPushConsumerConfigurationBuilder> config = null)
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            Func<ILinkRecievedMessage<object>, Task> onMessageHandler = message =>
            {
                onMessage(message);
                return Task.FromResult((object)null);
            };

            return @this.CreatePushConsumer(
                topologyConfiguration,
                onMessageHandler,
                parallel,
                configurationError,
                serializationError,
                config
                );
        }

        public static ILinkPushConsumer CreatePushConsumer(this Link @this,
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Func<ILinkRecievedMessage<byte[]>, Task> onMessage,
            bool parallel = true,
            Func<Exception, Task> configurationError = null,
            Action<Exception, ILinkRecievedMessage<byte[]>> serializationError = null,
            Action<ILinkPushConsumerConfigurationBuilder> config = null)
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            return @this.CreatePushConsumer(
                topologyConfiguration,
                cfg =>
                {
                    if (parallel)
                        cfg.AllParallelAsync(onMessage);
                    else
                        cfg.AllAsync(onMessage);
                },
                configurationError,
                serializationError,
                config
                );
        }

        public static ILinkPushConsumer CreatePushConsumer(this Link @this,
            Func<ILinkTopologyConfig, Task<ILinkQueue>> topologyConfiguration,
            Action<ILinkRecievedMessage<byte[]>> onMessage,
            bool parallel = true,
            Func<Exception, Task> configurationError = null,
            Action<Exception, ILinkRecievedMessage<byte[]>> serializationError = null,
            Action<ILinkPushConsumerConfigurationBuilder> config = null)
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            return @this.CreatePushConsumer(
                topologyConfiguration,
                message =>
                {
                    onMessage(message);
                    return Task.FromResult((object)null);
                },
                parallel,
                configurationError,
                serializationError,
                config
                );
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