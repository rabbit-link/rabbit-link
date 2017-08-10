#region Usings

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Channels;

#endregion

namespace RabbitLink.Internals.Actions
{
    internal class ActionStorage<TActor> : IActionStorage<TActor>
    {
        private readonly IChannel<ActionItem<TActor>> _channel;

        public ActionStorage(IChannel<ActionItem<TActor>> channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public async Task<T> PutAsync<T>(Func<TActor, T> action, CancellationToken cancellation)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var item = new ActionItem<TActor>(actor => action(actor), cancellation);

            await _channel.PutAsync(item)
                .ConfigureAwait(false);

            return (T) await item
                .Completion
                .ConfigureAwait(false);
        }

        public Task PutAsync(Action<TActor> action, CancellationToken cancellation)
            => PutAsync<object>(actor =>
            {
                action(actor);
                return null;
            }, cancellation);

        public ActionItem<TActor> Wait(CancellationToken cancellation)
            => _channel.Wait(cancellation);

        public Task<ActionItem<TActor>> WaitAsync(CancellationToken cancellation)
            => _channel.WaitAsync(cancellation);

        public void Dispose()
            => _channel.Dispose();
    }
}