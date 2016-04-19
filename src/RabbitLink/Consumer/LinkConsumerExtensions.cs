#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    public static class LinkConsumerExtensions
    {
        public static async Task<ILinkMessage<object>> GetMessageAsync(this ILinkConsumer @this,
            TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                return await @this.GetMessageAsync<object>(cts.Token)
                    .ConfigureAwait(false);
            }
        }

        public static async Task<ILinkMessage<T>> GetMessageAsync<T>(this ILinkConsumer @this,
            TimeSpan timeout) where T : class
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                return await @this.GetMessageAsync<T>(cts.Token)
                    .ConfigureAwait(false);                
            }            
        }      
    }
}