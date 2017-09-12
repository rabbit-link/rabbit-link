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
        /// <param name="cancellation">operation cancellation</param>
        Task<ILinkPulledMessage> GetMessageAsync(CancellationToken? cancellation = null);
    }
}