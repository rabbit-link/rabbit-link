using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitLink.Internals.Channels
{
    internal interface ICompositeChannel<T> : IChannel<T> where T : class, IChannelItem
    {
        void PutRetry(IEnumerable<T> items, CancellationToken cancellation);
        Task PutRetryAsync(IEnumerable<T> items, CancellationToken cancellation);
        void Yield(CancellationToken cancellation);
        Task YieldAsync(CancellationToken cancellation);
    }
}