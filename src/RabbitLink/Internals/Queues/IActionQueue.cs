#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal interface IActionQueue<TActor>
    {
        Task<T> PutAsync<T>(Func<TActor, T> action, CancellationToken cancellation);
        Task PutAsync(Action<TActor> action, CancellationToken cancellation);

        ActionQueueItem<TActor> Wait(CancellationToken cancellationToken);
        Task<ActionQueueItem<TActor>> WaitAsync(CancellationToken cancellationToken);

        void Complete();
    }
}