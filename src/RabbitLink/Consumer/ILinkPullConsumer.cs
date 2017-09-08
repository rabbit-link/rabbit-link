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
        /// <inheritdoc/>
        TimeSpan GetMessageTimeout { get; }

        /// <inheritdoc/>
        Task<ILinkPulledMessage> GetMessageAsync(CancellationToken? cancellation = null);
    }
}