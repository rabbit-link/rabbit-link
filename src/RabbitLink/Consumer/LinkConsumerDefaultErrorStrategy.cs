#region Usings

using System;

#endregion

namespace RabbitLink.Consumer
{
    /// <inheritdoc />
    /// <summary>
    ///     Default error strategy for <see cref="T:RabbitLink.Consumer.ILinkConsumer" />.
    ///     Nack message on exception.
    ///     Requeue message on handler task cancellation.
    /// </summary>
    public class LinkConsumerDefaultErrorStrategy : ILinkConsumerErrorStrategy
    {
        #region ILinkConsumerErrorStrategy Members

        /// <inheritdoc />
        /// <summary>
        ///     Nack message on any other exception.
        /// </summary>
        public LinkConsumerAckStrategy HandleError(Exception ex)
            => LinkConsumerAckStrategy.Nack;

        /// <inheritdoc />
        /// <summary>
        ///     Requeue message
        /// </summary>
        public LinkConsumerAckStrategy HandleCancellation()
            => LinkConsumerAckStrategy.Requeue;

        #endregion
    }
}
