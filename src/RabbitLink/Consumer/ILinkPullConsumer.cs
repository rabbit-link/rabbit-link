using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Messaging;

namespace RabbitLink.Consumer
{
    /// <summary>
    /// Represents RabbitMQ message consumer which manage internal message queue 
    /// and implements semantic
    /// </summary>
    public interface ILinkPullConsumer : ILinkConsumer
    {
        /// <summary>
        /// Timeout for <see cref="GetMessageAsync"/>
        /// </summary>
        TimeSpan GetMessageTimeout { get; }

        /// <summary>
        /// Get message from internal queue, waits if none.
        /// </summary>
        /// <typeparam name="TBody">
        /// Byte[] for get raw message.
        /// Object to use type name mapping, if mapping not found then Byte[] will be returned.
        /// Concrete type to deserialize all messages to it.
        /// </typeparam>
        /// <param name="cancellation">operation cancellation</param>
        Task<ILinkPulledMessage<TBody>> GetMessageAsync<TBody>(CancellationToken? cancellation = null)
            where TBody : class;
    }
}
