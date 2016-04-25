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

        private class JobItem
        {
            private readonly IDisposable _cancellationRegistration;
            private readonly TaskCompletionSource<object> _completion = new TaskCompletionSource<object>();
            private readonly Func<Task<object>> _action;
            private readonly CancellationToken _cancellation;

            public JobItem(Func<Task<object>> action, CancellationToken cancellationToken)
            {
                _action = action;
                _cancellation = cancellationToken;

                try
                {
                    _cancellationRegistration = _cancellation.Register(() =>
                    {
                        _completion.TrySetCanceled();
                    });
                }
                catch (ObjectDisposedException)
                {
                    // Cancellation source already disposed                    
                }

                if (_cancellation.IsCancellationRequested)
                {
                    _completion.TrySetCanceled();
                    _cancellationRegistration?.Dispose();
                }
            }

            public Task<object> Task => _completion.Task;

            public async Task RunAsync()
            {
                try
                {
                    if (_cancellation.IsCancellationRequested)
                    {
                        _completion.TrySetCanceled();
                    }
                    else
                    {
                        try
                        {
                            _completion.TrySetResult(await _action().ConfigureAwait(false));
                        }
                        catch (Exception ex)
                        {
                            _completion.TrySetException(ex);
                        }
                    }
                }
                finally
                {
                    _cancellationRegistration?.Dispose();
                }
            }

            public void SetException(Exception exception)
            {
                try
                {
                    if (_cancellation.IsCancellationRequested)
                    {
                        _completion.TrySetCanceled();
                    }
                    else
                    {
                        _completion.TrySetException(exception);
                    }
                }
                finally
                {
                    _cancellationRegistration?.Dispose();
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
            _loopTask = LoopAsync();
        }

        public EventLoop(int maxEvents, DisposingStrategy strategy = DisposingStrategy.Throw)
        {
            _strategy = strategy;
            _jobQueue = new AsyncProducerConsumerQueue<JobItem>(maxEvents);
            _loopTask = LoopAsync();
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
                    async () =>
                    {
                        await Task.Delay(0)
                            .ConfigureAwait(false);

                        return await action()
                            .ConfigureAwait(false);
                    },                        
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
            return ScheduleAsync(async () =>
            {
                await Task.Delay(0)
                    .ConfigureAwait(false);

                return action();
            }, token);
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