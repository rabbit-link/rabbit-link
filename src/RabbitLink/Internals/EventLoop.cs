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

            _cancellation.Cancel();
            _loopTask.WaitAndUnwrapException();
        }

        #endregion

        #region Loop

        private async Task Loop()
        {
            while (_strategy == DisposingStrategy.Wait || !_cancellation.IsCancellationRequested)
            {
                JobItem job;
                try
                {
                    job = await _jobQueue.DequeueAsync(_cancellation.Token)
                        .ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                job.Processing = true;
                if (job.CancellationToken.IsCancellationRequested)
                {
                    job.Completion.TrySetCanceled();
                    continue;
                }

                try
                {
                    job.Completion.TrySetResult(await job.Action().ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    job.Completion.TrySetException(ex);
                }
            }

            while (true)
            {
                JobItem job;
                try
                {
                    job = await _jobQueue.DequeueAsync(_cancellation.Token)
                        .ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                if (job.CancellationToken.IsCancellationRequested)
                {
                    job.Completion.TrySetCanceled();
                }
                else
                {
                    job.Completion.TrySetException(new ObjectDisposedException(GetType().Name));
                }
            }
        }

        #endregion

        #region Private classes

        private class JobItem
        {
            public JobItem(Func<Task<object>> action, CancellationToken cancellationToken)
            {
                Action = action;
                CancellationToken = cancellationToken;
            }

            public bool Processing { get; set; }
            public CancellationToken CancellationToken { get; }
            public Func<Task<object>> Action { get; }
            public TaskCompletionSource<object> Completion { get; } = new TaskCompletionSource<object>();
        }

        #endregion

        #region .ctor        

        public EventLoop(DisposingStrategy strategy = DisposingStrategy.Throw)
        {
            _strategy = strategy;
            _jobQueue = new AsyncProducerConsumerQueue<JobItem>();
            _loopTask = Loop();
        }

        public EventLoop(int maxEvents, DisposingStrategy strategy = DisposingStrategy.Throw)
        {
            _strategy = strategy;
            _jobQueue = new AsyncProducerConsumerQueue<JobItem>(maxEvents);
            _loopTask = Loop();
        }

        #endregion

        #region Fields

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly AsyncProducerConsumerQueue<JobItem> _jobQueue;
        private readonly Task _loopTask;
        private readonly DisposingStrategy _strategy;

        #endregion

        #region Schedule

        public async Task<T> Schedule<T>(Func<Task<T>> action, CancellationToken token)
        {
            if (_cancellation.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().Name);

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var job =
                new JobItem(
                    async () =>
                        await Task.Run(async () => await action().ConfigureAwait(false), token).ConfigureAwait(false),
                    token);

            token.Register(() =>
            {
                if (!job.Processing)
                {
                    job.Completion.TrySetCanceled();
                }
            });

            try
            {
                await _jobQueue.EnqueueAsync(job, token)
                    .ConfigureAwait(false);
            }
            catch
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return (T) await job.Completion.Task
                .ConfigureAwait(false);
        }

        public Task<T> Schedule<T>(Func<Task<T>> action)
        {
            return Schedule(action, CancellationToken.None);
        }

        public Task Schedule(Func<Task> action, CancellationToken token)
        {
            return Schedule(async () =>
            {
                await action().ConfigureAwait(false);
                return (object) null;
            }, token);
        }

        public Task Schedule(Func<Task> action)
        {
            return Schedule(action, CancellationToken.None);
        }

        public Task<T> Schedule<T>(Func<T> action, CancellationToken token)
        {
            return Schedule(async () => await Task.Run(action, token)
                .ConfigureAwait(false), token);
        }

        public Task<T> Schedule<T>(Func<T> action)
        {
            return Schedule(action, CancellationToken.None);
        }

        public Task Schedule(Action action, CancellationToken token)
        {
            Func<object> fn = () =>
            {
                action();
                return null;
            };

            return Schedule(fn, token);
        }

        public Task Schedule(Action action)
        {
            return Schedule(action, CancellationToken.None);
        }

        #endregion
    }
}