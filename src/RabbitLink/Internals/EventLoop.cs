#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;

#endregion

namespace RabbitLink.Internals
{
    internal class EventLoop : IDisposable
    {
        /// <summary>
        /// </summary>
        public enum DisposingStrategy
        {
            /// <summary>
            ///     Throws ObjectDisposingException on events
            ///     which is not completed on Dispose
            /// </summary>
            Throw,

            /// <summary>
            ///     Waits for all already scheduled events
            ///     will be processed
            /// </summary>
            Wait
        }

        #region IDisposable

        public void Dispose()
        {
            if (_cancellation.IsCancellationRequested)
                return;

            _cancellationSource.Cancel();
            _cancellationSource.Dispose();

            // ReSharper disable once MethodSupportsCancellation
            _loopTask.WaitAndUnwrapException();
            _loopTask.Dispose();

            _jobQueue.Dispose();
        }

        #endregion

        #region Loop

        private async Task LoopAsync()
        {
            while (_strategy == DisposingStrategy.Wait || !_cancellation.IsCancellationRequested)
            {
                JobItem job;
                try
                {
                    job = await _jobQueue.DequeueAsync(_cancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                // ReSharper disable once MethodSupportsCancellation
                await job.RunAsync()
                .ConfigureAwait(false);
            }

            while (true)
            {
                JobItem job;
                try
                {
                    job = await _jobQueue.DequeueAsync(_cancellation)
                        .ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                job.SetException(new ObjectDisposedException(GetType().Name));
            }
        }

        #endregion

        #region Private classes

        private class JobItem : LinkQueueMessage<object>
        {
            private readonly Func<Task<object>> _action;

            public JobItem(Func<Task<object>> action, CancellationToken cancellation) : base(cancellation)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                _action = action;
            }

            public async Task RunAsync()
            {
                try
                {
                    SetResult(await _action().ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    SetException(ex);
                }
            }
        }

        #endregion

        #region .ctor        

        public EventLoop(DisposingStrategy strategy = DisposingStrategy.Throw)
        {
            _cancellationSource = new CancellationTokenSource();
            _cancellation = _cancellationSource.Token;
            _strategy = strategy;
            _jobQueue = new AsyncProducerConsumerQueue<JobItem>();
            _loopTask = Task.Run(async () => await LoopAsync().ConfigureAwait(false));
        }

        public EventLoop(int maxEvents, DisposingStrategy strategy = DisposingStrategy.Throw)
        {
            _strategy = strategy;
            _jobQueue = new AsyncProducerConsumerQueue<JobItem>(maxEvents);
            _loopTask = Task.Run(async () => await LoopAsync().ConfigureAwait(false));
        }

        #endregion

        #region Fields

        private readonly CancellationTokenSource _cancellationSource;
        private readonly CancellationToken _cancellation;
        private readonly AsyncProducerConsumerQueue<JobItem> _jobQueue;
        private readonly Task _loopTask;
        private readonly DisposingStrategy _strategy;

        #endregion

        #region Schedule

        public async Task<T> ScheduleAsync<T>(Func<Task<T>> action, CancellationToken token)
        {
            if (_cancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var job =
                new JobItem(
                    async () => await Task.Run(async ()=> await action().ConfigureAwait(false), token).ConfigureAwait(false),
                    token);

            try
            {
                await _jobQueue.EnqueueAsync(job, token)
                    .ConfigureAwait(false);
            }
            catch
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return (T)await job.Task
                .ConfigureAwait(false);
        }

        public Task ScheduleAsync(Func<Task> action, CancellationToken token)
        {
            return ScheduleAsync(async () =>
            {
                await action().ConfigureAwait(false);
                return (object)null;
            }, token);
        }

        public Task<T> ScheduleAsync<T>(Func<T> action, CancellationToken token)
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            return ScheduleAsync(async () => action(), token);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        }

        public Task ScheduleAsync(Action action, CancellationToken token)
        {
            Func<object> fn = () =>
            {
                action();
                return null;
            };

            return ScheduleAsync(fn, token);
        }

        public Task ScheduleAsync(Action action)
        {
            return ScheduleAsync(action, CancellationToken.None);
        }

        #endregion
    }
}