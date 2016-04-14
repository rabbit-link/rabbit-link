#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx.Synchronous;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Producer
{
    public static class LinkProducerExtensions
    {
        #region Async

        public static async Task PublishAsync(this ILinkProducer @this, ILinkMessage<byte[]> message,
            LinkPublishProperties properties,
            TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                await @this.PublishAsync(message, properties, cts.Token)
                    .ConfigureAwait(false);
            }
        }

        public static async Task PublishAsync<T>(this ILinkProducer @this, ILinkMessage<T> message,
            LinkPublishProperties properties,
            TimeSpan timeout) where T : class
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                await @this.PublishAsync(message, properties, cts.Token)
                    .ConfigureAwait(false);
            }            
        }

        public static async Task PublishAsync(this ILinkProducer @this, ILinkMessage<byte[]> message, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                await @this.PublishAsync(message, cancellation: cts.Token)
                    .ConfigureAwait(false);
            }         
        }

        public static async Task PublishAsync<T>(this ILinkProducer @this, ILinkMessage<T> message, TimeSpan timeout)
            where T : class
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                await @this.PublishAsync(message, cancellation: cts.Token)
                    .ConfigureAwait(false);
            }            
        }

        #endregion

        #region Sync

        public static void Publish(this ILinkProducer @this, ILinkMessage<byte[]> message,
            LinkPublishProperties properties,
            TimeSpan timeout)
        {
            @this.PublishAsync(message, properties, timeout)
                .WaitAndUnwrapException();
        }

        public static void Publish<T>(this ILinkProducer @this, ILinkMessage<T> message,
            LinkPublishProperties properties,
            TimeSpan timeout) where T : class
        {
            @this.PublishAsync(message, properties, timeout)
                .WaitAndUnwrapException();
        }

        public static void Publish(this ILinkProducer @this, ILinkMessage<byte[]> message,
            LinkPublishProperties properties)
        {
            @this.PublishAsync(message, properties)
                .WaitAndUnwrapException();
        }

        public static void Publish<T>(this ILinkProducer @this, ILinkMessage<T> message,
            LinkPublishProperties properties) where T : class
        {
            @this.PublishAsync(message, properties)
                .WaitAndUnwrapException();
        }

        public static void Publish(this ILinkProducer @this, ILinkMessage<byte[]> message, TimeSpan timeout)
        {
            @this.PublishAsync(message, timeout)
                .WaitAndUnwrapException();
        }

        public static void Publish<T>(this ILinkProducer @this, ILinkMessage<T> message, TimeSpan timeout)
            where T : class
        {
            @this.PublishAsync(message, timeout)
                .WaitAndUnwrapException();
        }

        public static void Publish(this ILinkProducer @this, ILinkMessage<byte[]> message)
        {
            @this.PublishAsync(message)
                .WaitAndUnwrapException();
        }

        public static void Publish<T>(this ILinkProducer @this, ILinkMessage<T> message) where T : class
        {
            @this.PublishAsync(message)
                .WaitAndUnwrapException();
        }

        #endregion
    }
}