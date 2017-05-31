#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal class ActionQueueInvoker<TActor> : IActionInvoker<TActor>
    {
        #region Fields

        private readonly CancellationToken _cancellation;
        private readonly ActionQueue<TActor> _queue;

        #endregion

        #region Ctor

        public ActionQueueInvoker(ActionQueue<TActor> queue, CancellationToken cancellation)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _cancellation = cancellation;
        }

        public ActionQueueInvoker(ActionQueue<TActor> queue) : this(queue, CancellationToken.None)
        {
        }

        #endregion

        #region IActionInvoker<TActor> Members

        public async Task InvokeAsync(Action<TActor> action, CancellationToken cancellation)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellation, cancellation))
            {
                await _queue.PutAsync(action, cts.Token)
                    .ConfigureAwait(false);
            }
        }

        public async Task<T> InvokeAsync<T>(Func<TActor, T> action, CancellationToken cancellation)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellation, cancellation))
            {
                return await _queue.PutAsync(action, cts.Token)
                    .ConfigureAwait(false);
            }
        }

        #endregion
    }
}