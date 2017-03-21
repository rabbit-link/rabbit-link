using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace RabbitLink.Internals
{
    internal class LinkQueueMessage : LinkAbstractQueueMessage
    {
        private readonly TaskCompletionSource<object> _completion =
            TaskCompletionSourceExtensions.CreateAsyncTaskSource<object>();

        public LinkQueueMessage(CancellationToken cancellation) : base(cancellation)
        {
        }

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

        public Task Task => _completion.Task;
    }

    internal class LinkQueueMessage<TResult> : LinkAbstractQueueMessage
    {
        private readonly TaskCompletionSource<TResult> _completion =
            TaskCompletionSourceExtensions.CreateAsyncTaskSource<TResult>();

        public LinkQueueMessage(CancellationToken cancellation) : base(cancellation)
        {
        }

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

        public Task<TResult> Task => _completion.Task;
    }
}