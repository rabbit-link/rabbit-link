#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal interface IActionInvoker<out TActor>
    {
        Task InvokeAsync(Action<TActor> action, CancellationToken cancellation);
        Task<T> InvokeAsync<T>(Func<TActor, T> action, CancellationToken cancellation);
    }
}