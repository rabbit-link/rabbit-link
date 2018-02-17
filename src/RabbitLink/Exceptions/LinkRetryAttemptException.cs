using System;

namespace RabbitLink.Exceptions
{
    /// <summary>
    /// When thrown in serve handler, indicate needing of retry
    /// </summary>
    public class LinkRetryAttemptException : Exception
    {
        /// <summary>
        /// Immediate retry attempt (requeue)
        /// </summary>
        public LinkRetryAttemptException()
        {
            RetryAfter = TimeSpan.Zero;
        }

        /// <summary>
        /// Wait requeue
        /// </summary>
        /// <param name="retryAfter">wait interval</param>
        /// <exception cref="ArgumentOutOfRangeException">interval must be non-negative</exception>
        public LinkRetryAttemptException(TimeSpan retryAfter)
        {
            if(retryAfter < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryAfter), retryAfter, "Time to retry must be non negative value");
            RetryAfter = retryAfter;
        }

        /// <summary>
        /// Wait requeue
        /// </summary>
        /// <param name="retryAfter">wait interval in milliseconds</param>
        /// <exception cref="ArgumentOutOfRangeException">interval must be non-negative</exception>
        public LinkRetryAttemptException(int retryAfter)
            : this(TimeSpan.FromMilliseconds(retryAfter))
        {

        }

        /// <summary>
        /// Requeue interval
        /// </summary>
        public TimeSpan RetryAfter { get; }
    }
}
