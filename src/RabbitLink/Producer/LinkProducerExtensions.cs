#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Producer
{
    public static class LinkProducerExtensions
    {        
        public static async Task PublishAsync(this ILinkProducer @this, byte[] body,
            LinkMessageProperties properties,
            LinkPublishProperties publishProperties,
            TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                await @this.PublishAsync(body, properties, publishProperties, cts.Token)
                    .ConfigureAwait(false);
            }
        }

        public static async Task PublishAsync<T>(this ILinkProducer @this, T body,
            LinkMessageProperties properties,
            LinkPublishProperties publishProperties,
            TimeSpan timeout) where T : class
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                await @this.PublishAsync(body, properties, publishProperties, cts.Token)
                    .ConfigureAwait(false);
            }
        }

        public static Task PublishAsync(this ILinkProducer @this, byte[] body,
            LinkMessageProperties properties,
            TimeSpan timeout)
        {
            return @this.PublishAsync(body, properties, null, timeout);
        }

        public static Task PublishAsync<T>(this ILinkProducer @this, T body,
            LinkMessageProperties properties,
            TimeSpan timeout) where T : class
        {
            return @this.PublishAsync(body, properties, null, timeout);
        }

        public static Task PublishAsync(this ILinkProducer @this, byte[] body,
            LinkPublishProperties publishProperties,
            TimeSpan timeout)
        {
            return @this.PublishAsync(body, null, publishProperties, timeout);
        }

        public static Task PublishAsync<T>(this ILinkProducer @this, T body,
            LinkPublishProperties publishProperties,
            TimeSpan timeout) where T : class
        {
            return @this.PublishAsync(body, null, publishProperties, timeout);
        }

        public static Task PublishAsync(this ILinkProducer @this, byte[] body,            
            TimeSpan timeout)
        {
            return @this.PublishAsync(body, null, null, timeout);
        }

        public static Task PublishAsync<T>(this ILinkProducer @this, T body,            
            TimeSpan timeout) where T : class
        {
            return @this.PublishAsync(body, null, null, timeout);
        }               
    }
}