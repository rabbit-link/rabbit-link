#region Usings

using System;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    /// <summary>
    ///     Default error strategy.
    ///     For <see cref="ILinkConsumerErrorStrategy.OnHandlerError{T}" />:
    ///     If exception is <see cref="LinkConsumerNackMessageException" />
    ///     and <see cref="LinkConsumerNackMessageException.Requeue" /> is true
    ///     then returns <see cref="LinkConsumerAckStrategy.NackWithRequeue" />
    ///     else returns <see cref="LinkConsumerAckStrategy.Nack" />
    ///     For <see cref="ILinkConsumerErrorStrategy.OnHandlerCancelled{T}" />:
    ///     returns <see cref="LinkConsumerAckStrategy.Nack" />
    /// </summary>
    public class DefaultConsumerErrorStrategy : ILinkConsumerErrorStrategy
    {
        public virtual LinkConsumerAckStrategy OnHandlerError<T>(ILinkRecievedMessage<T> message, Exception exception)
            where T : class
        {
            var nackedException = exception as LinkConsumerNackMessageException;

            return nackedException?.Requeue != true
                ? LinkConsumerAckStrategy.NackWithRequeue
                : LinkConsumerAckStrategy.Nack;
        }

        public virtual LinkConsumerAckStrategy OnHandlerCancelled<T>(ILinkRecievedMessage<T> message) where T : class
        {
            return LinkConsumerAckStrategy.NackWithRequeue;
        }
    }
}