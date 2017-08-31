using System;
using System.Threading;
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
        /// Timeout for <see cref="GetMessage(System.Nullable{System.Threading.CancellationToken})"/> method, 
        /// TimeSpan.Zero = infinite
        /// </summary>
        TimeSpan GetMessageTimeout { get; }

        /// <summary>
        /// Gets message from internal queue, waits if none
        /// </summary>
        /// <param name="cancellation">If null then <see cref="GetMessageTimeout"/> will be used</param>
        ILinkPulledMessage GetMessage(CancellationToken? cancellation = null);
    }
}