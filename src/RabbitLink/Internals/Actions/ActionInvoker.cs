#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Actions
{
    internal class ActionInvoker<TActor> : IActionInvoker<TActor>
    {
        #region Fields

        private readonly CancellationToken _cancellation;
        private readonly IActionStorage<TActor> _storage;

        #endregion

        #region Ctor

        public ActionInvoker(IActionStorage<TActor> storage, CancellationToken cancellation)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _cancellation = cancellation;
        }

        public ActionInvoker(IActionStorage<TActor> storage) : this(storage, CancellationToken.None)
        {
        }

        #endregion

        #region IActionInvoker<TActor> Members

        public async Task InvokeAsync(Action<TActor> action, CancellationToken cancellation)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellation, cancellation))
            {
                await _storage.PutAsync(action, cts.Token)
                    .ConfigureAwait(false);
            }
        }

        public async Task<T> InvokeAsync<T>(Func<TActor, T> action, CancellationToken cancellation)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellation, cancellation))
            {
                return await _storage.PutAsync(action, cts.Token)
                    .ConfigureAwait(false);
            }
        }

        #endregion
    }
}
