#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Channels
{
    internal class ChannelItem<TValue, TResult> : IChannelItem
    {
        #region Fields

        private readonly TaskCompletionSource<TResult> _tcs =
            new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        #endregion

        #region Ctor

        public ChannelItem(TValue value, CancellationToken cancellationToken)
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

    internal class ChannelItem<TValue> : ChannelItem
    {
        #region Ctor

        public ChannelItem(TValue value, CancellationToken cancellationToken) : base(cancellationToken)
        {
            Value = value;
        }

        #endregion

        #region Properties

        public TValue Value { get; }

        #endregion
    }

    internal abstract class ChannelItem : IChannelItem
    {
        #region Fields

        private readonly TaskCompletionSource<object> _tcs =
            new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        #endregion

        #region Ctor

        protected ChannelItem(CancellationToken cancellationToken)
        {
            Cancellation = cancellationToken;
        }

        #endregion

        #region Properties

        public Task Completion => _tcs.Task;

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

        public bool TrySetResult()
        {
            return _tcs.TrySetResult(null);
        }
    }
}
