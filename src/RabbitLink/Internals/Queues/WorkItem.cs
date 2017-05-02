#region Usings

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Internals.Queues
{
    class WorkItem<TValue, TResult>
    {
        public WorkItem(TValue value, CancellationToken cancellationToken)
        {
            Cancellation = cancellationToken;
            Value = value;
        }

        public CancellationToken Cancellation { get; }

        public TaskCompletionSource<TResult> Completion { get; } =
            new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        public TValue Value { get; }
    }
}