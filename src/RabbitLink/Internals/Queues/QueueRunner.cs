#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Queues
{

    internal class QueueRunner<TActor> : IDisposable
    {
        #region Fields

        private readonly WorkQueue<Item> _queue = new WorkQueue<Item>();

        #endregion

        private Action<Item, Exception> _exceptionHandler;

        public QueueRunner()
        {
            _exceptionHandler = (item, ex) => item.TrySetException(ex);
        }

        public QueueRunner(Action<Item,Exception> exceptionHandler)
        {
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(new ObjectDisposedException(GetType().Name));
        }

        #endregion


        public void Enqueue(Item item)
        {
            _queue.Put(item);
        }

        public void EnqueueRetry(IEnumerable<Item> items, CancellationToken cancellation)
        {
            _queue.PutRetry(items, cancellation);
        }

        public Task EnqueueRetryAsync(IEnumerable<Item> items,
            CancellationToken cancellation)
        {
            return _queue.PutRetryAsync(items, cancellation);
        }

        public async Task<T> EnqueueAsync<T>(Func<TActor, T> action, CancellationToken cancellation)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var item = new Item(actor => action(actor), cancellation);

            Enqueue(item);

            return (T) await item
                .Completion
                .ConfigureAwait(false);
        }

        public Task EnqueueAsync(Action<TActor> action, CancellationToken cancellation)
        {
            return EnqueueAsync<object>(actor =>
            {
                action(actor);
                return null;
            }, cancellation);
        }

        public void Run(TActor actor, CancellationToken cancellation)
        {
            while (true)
            {
                var item = _queue.Wait(cancellation);
                try
                {
                    var result = item.Value(actor);
                    item.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    item.TrySetException(ex);
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public async Task RunAsync(TActor actor, bool useThread, CancellationToken cancellation)
        {
            if (useThread)
            {
                await Task.Factory.StartNew(() =>
                    {
                        Run(actor, cancellation);
                    }, cancellation, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ConfigureAwait(false);
            }
            else
            {
                while (true)
                {
                    var item = await _queue.WaitAsync(cancellation)
                        .ConfigureAwait(false);

                    await Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                var result = item.Value(actor);
                                item.TrySetResult(result);
                            }
                            catch (Exception ex)
                            {
                                item.TrySetException(ex);
                            }
                        }, cancellation)
                        .ConfigureAwait(false);
                }
            }
        }

        public Task YieldAsync(CancellationToken cancellation)
        {
            return _queue.YieldAsync(cancellation);
        }

        public void Dispose(Exception ex)
        {
            _queue.Complete(t => t.TrySetException(ex));
        }


        public class Item : WorkItem<Func<TActor, object>, object>
        {
            public Item(Func<TActor, object> value, CancellationToken cancellationToken) : base(value, cancellationToken)
            {
            }
        }
    }
}