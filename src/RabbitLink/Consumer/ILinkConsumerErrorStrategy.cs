#region Usings

using System;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    public interface ILinkConsumerErrorStrategy
    {
        /// <summary>
        ///     This method is fired when an exception is thrown. Implement a strategy for
        ///     handling the exception here.
        /// </summary>
        /// <param name="message">Message recieved</param>
        /// <param name="exception">Exception fired by handler</param>
        /// <returns><see cref="LinkConsumerAckStrategy" /> for processing the original failed message</returns>
        LinkConsumerAckStrategy OnHandlerError<T>(ILinkRecievedMessage<T> message, Exception exception) where T : class;

        /// <summary>
        ///     This method is fired when the task returned from the UserHandler is cancelled.
        ///     Implement a strategy for handling the cancellation here.
        /// </summary>
        /// <param name="message">Message recieved</param>
        /// <returns><see cref="LinkConsumerAckStrategy" /> for processing the original cancelled message</returns>
        LinkConsumerAckStrategy OnHandlerCancelled<T>(ILinkRecievedMessage<T> message) where T : class;
    }
}