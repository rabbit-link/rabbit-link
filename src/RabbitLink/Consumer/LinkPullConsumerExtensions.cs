#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx.Synchronous;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    public static class LinkPullConsumerExtensions
    {
        public static async Task<ILinkAckableRecievedMessage<object>> GetMessageAsync(this ILinkPullConsumer @this,
            TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                return await @this.GetMessageAsync<object>(cts.Token)
                    .ConfigureAwait(false);
            }
        }

        public static async Task<ILinkAckableRecievedMessage<T>> GetMessageAsync<T>(this ILinkPullConsumer @this,
            TimeSpan timeout) where T : class
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                return await @this.GetMessageAsync<T>(cts.Token)
                    .ConfigureAwait(false);                
            }            
        }

        public static ILinkAckableRecievedMessage<object> GetMessage(this ILinkPullConsumer @this, TimeSpan timeout)
        {
            return @this.GetMessageAsync(timeout)
                .WaitAndUnwrapException();
        }

        public static ILinkAckableRecievedMessage<T> GetMessage<T>(this ILinkPullConsumer @this, TimeSpan timeout)
            where T : class
        {
            return @this.GetMessageAsync<T>(timeout)
                .WaitAndUnwrapException();
        }

        public static ILinkAckableRecievedMessage<object> GetMessage(this ILinkPullConsumer @this)
        {
            return @this.GetMessageAsync()
                .WaitAndUnwrapException();
        }

        public static ILinkAckableRecievedMessage<T> GetMessage<T>(this ILinkPullConsumer @this) where T : class
        {
            return @this.GetMessageAsync<T>()
                .WaitAndUnwrapException();
        }
    }
}