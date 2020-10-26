using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Channels;

namespace RabbitLink.Internals.Actions
{
    internal class CompositeActionStorage<TActor> : ICompositeActionStorage<TActor>
    {
        private readonly ICompositeChannel<ActionItem<TActor>> _channel;

        public CompositeActionStorage(ICompositeChannel<ActionItem<TActor>> channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public async Task<T> PutAsync<T>(Func<TActor, T> action, CancellationToken cancellation)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var item = new ActionItem<TActor>(actor => action(actor)!, cancellation);

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
                return null!;
            }, cancellation);

        public ActionItem<TActor> Wait(CancellationToken cancellation)
            => _channel.Wait(cancellation);

        public Task<ActionItem<TActor>> WaitAsync(CancellationToken cancellation)
            => _channel.WaitAsync(cancellation);

        public void Dispose()
            => _channel.Dispose();

        public void PutRetry(IEnumerable<ActionItem<TActor>> items, CancellationToken cancellation)
            => _channel.PutRetry(items, cancellation);

        public Task PutRetryAsync(IEnumerable<ActionItem<TActor>> items, CancellationToken cancellation)
            => _channel.PutRetryAsync(items, cancellation);

        public void Yield(CancellationToken cancellation)
            => _channel.Yield(cancellation);

        public Task YieldAsync(CancellationToken cancellation)
            => _channel.YieldAsync(cancellation);
    }
}
