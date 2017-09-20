#region Usings

using System;
using RabbitLink.Exceptions;

#endregion

namespace RabbitLink.Consumer
{
    /// <summary>
    ///     Default error strategy for <see cref="ILinkConsumer" />.
    ///     Nacks or requeue message on <see cref="LinkConsumerNackException" />.
    ///     Nack message on any other exception.
    ///     Requeue message on handler task cancellation.
    /// </summary>
    public class LinkConsumerDefaultErrorStrategy : ILinkConsumerErrorStrategy
    {
        #region ILinkConsumerErrorStrategy Members

        /// <summary>
        ///     Nacks or requeue message on <see cref="LinkConsumerNackException" />.
        ///     Nack message on any other exception.
        /// </summary>
        public LinkConsumerAckStrategy HandleError(Exception ex)
        {
            if (ex is LinkConsumerNackException nackEx)
            {
                return nackEx.Requeue
                    ? LinkConsumerAckStrategy.Requeue
                    : LinkConsumerAckStrategy.Nack;
            }

            return LinkConsumerAckStrategy.Nack;
        }

        /// <summary>
        ///     Requeue message
        /// </summary>
        public LinkConsumerAckStrategy HandleCancellation()
        {
            return LinkConsumerAckStrategy.Requeue;
        }

        #endregion
    }
}