#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Async
{
    internal static class TaskExtensions
    {
        /// <summary>
        ///     Waits for the task to complete, but does not raise task exceptions. The task exception (if any) is unobserved.
        /// </summary>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        public static void WaitWithoutException(this Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            try
            {
                task.Wait();
            }
            catch (AggregateException)
            {
            }
        }

        /// <summary>
        /// Waits for the task to complete, unwrapping any exceptions.
        /// </summary>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        public static void WaitAndUnwrapException(this Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Waits for the task to complete, unwrapping any exceptions.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the task.</typeparam>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        /// <returns>The result of the task.</returns>
        public static TResult WaitAndUnwrapException<TResult>(this Task<TResult> task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates <see cref="Task"/> which will be completed on token cancel
        /// </summary>
        /// <param name="token">token watch on</param>
        /// <returns></returns>
        public static Task WaitCancellation(this CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled(token);

            var tcs = new TaskCompletionSource<object>();
            token.Register(() => tcs.TrySetResult(null));
            return tcs.Task;
        }
    }
}