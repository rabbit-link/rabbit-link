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

        public static Task PublishAsync(this ILinkProducer @this, ILinkMessage<byte[]> message,
            LinkPublishProperties properties,
            TimeSpan timeout)
        {
            return @this.PublishAsync(message, properties, new CancellationTokenSource(timeout).Token);
        }

        public static Task PublishAsync<T>(this ILinkProducer @this, ILinkMessage<T> message,
            LinkPublishProperties properties,
            TimeSpan timeout) where T : class
        {
            return @this.PublishAsync(message, properties, new CancellationTokenSource(timeout).Token);
        }

        public static Task PublishAsync(this ILinkProducer @this, ILinkMessage<byte[]> message, TimeSpan timeout)
        {
            return @this.PublishAsync(message, cancellation: new CancellationTokenSource(timeout).Token);
        }

        public static Task PublishAsync<T>(this ILinkProducer @this, ILinkMessage<T> message, TimeSpan timeout)
            where T : class
        {
            return @this.PublishAsync(message, cancellation: new CancellationTokenSource(timeout).Token);
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