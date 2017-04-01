#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals
{
    internal class LinkQueueMessage : LinkAbstractQueueMessage
    {
        private readonly TaskCompletionSource<object> _completion =
            new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public LinkQueueMessage(CancellationToken cancellation) : base(cancellation)
        {
        }

        public Task Task => _completion.Task;

        public override void SetException(Exception exception)
        {
            _completion.TrySetException(exception);
        }

        public override void SetCancelled()
        {
            _completion.TrySetCanceled();
        }

        public void SetResult()
        {
            _completion.TrySetResult(null);
        }
    }

    internal class LinkQueueMessage<TResult> : LinkAbstractQueueMessage
    {
        private readonly TaskCompletionSource<TResult> _completion =
            new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        public LinkQueueMessage(CancellationToken cancellation) : base(cancellation)
        {
        }

        public Task<TResult> Task => _completion.Task;

        public override void SetException(Exception exception)
        {
            _completion.TrySetException(exception);
        }

        public override void SetCancelled()
        {
            _completion.TrySetCanceled();
        }

        public void SetResult(TResult result)
        {
            _completion.TrySetResult(result);
        }
    }
}