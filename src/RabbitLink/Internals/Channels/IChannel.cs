using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitLink.Internals.Channels
{
    internal interface IChannel<T> : IDisposable where T : class, IChannelItem
    {
        void Put(T item);
        Task PutAsync(T item);
        T Wait(CancellationToken cancellation);
        Task<T> WaitAsync(CancellationToken cancellation);
        T Spin(CancellationToken cancellation);
        Task<T> SpinAsync(CancellationToken cancellation);
    }
}