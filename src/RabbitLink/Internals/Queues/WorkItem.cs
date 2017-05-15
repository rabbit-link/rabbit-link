#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Queues
{
    class WorkItem<TValue, TResult> : IWorkQueueItem
    {
        #region Fields

        private readonly TaskCompletionSource<TResult> _tcs =
            new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        #endregion

        #region Ctor

        public WorkItem(TValue value, CancellationToken cancellationToken)
        {
            Cancellation = cancellationToken;
            Value = value;
        }

        #endregion

        #region Properties

        public Task<TResult> Completion => _tcs.Task;

        public TValue Value { get; }

        #endregion

        #region IWorkQueueItem Members

        public CancellationToken Cancellation { get; }

        public bool TrySetException(Exception ex)
        {
            return _tcs.TrySetException(ex);
        }

        public bool TrySetCanceled(CancellationToken cancellation)
        {
            return _tcs.TrySetCanceled(cancellation);
        }

        #endregion

        public bool TrySetResult(TResult result)
        {
            return _tcs.TrySetResult(result);
        }
    }
}