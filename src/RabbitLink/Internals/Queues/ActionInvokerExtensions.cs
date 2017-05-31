#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal static class ActionInvokerExtensions
    {
        public static Task InvokeAsync<TActor>(this IActionInvoker<TActor> @this, Action<TActor> action)
        {
            return @this.InvokeAsync(action, CancellationToken.None);
        }

        public static Task<T> InvokeAsync<TActor, T>(this IActionInvoker<TActor> @this, Func<TActor, T> action)
        {
            return @this.InvokeAsync(action, CancellationToken.None);
        }
    }
}