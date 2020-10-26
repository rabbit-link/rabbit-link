#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Producer
{
    /// <summary>
    /// Represents RabbitMQ message producer
    /// </summary>
    public interface ILinkProducer : IDisposable
    {
        #region Properties

        /// <summary>
        /// Producer Id
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Confirm mode (see https://www.rabbitmq.com/confirms.html)
        /// </summary>
        bool ConfirmsMode { get; }

        /// <summary>
        /// Publish operation default timeout
        /// </summary>
        TimeSpan? PublishTimeout { get; }

        /// <summary>
        /// Operational state
        /// </summary>
        LinkProducerState State { get; }

        #endregion

        /// <summary>
        /// Waits for producer ready
        /// </summary>
        Task WaitReadyAsync(CancellationToken? cancellation = null);

        /// <summary>
        /// Publishes RAW message
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="cancellation">cancellation token, if null <see cref="Timeout" /> will be used</param>
        /// <returns><see cref="Task" /> which completed when message ACKed by broker</returns>
        Task PublishAsync(
            ILinkPublishMessage<byte[]> message,
            CancellationToken? cancellation = null
        );

        /// <summary>
        /// Serialize message body and publishes message
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="cancellation">cancellation token, if null <see cref="Timeout" /> will be used</param>
        /// <returns><see cref="Task" /> which completed when message ACKed by broker</returns>
        Task PublishAsync<TBody>(
            ILinkPublishMessage<TBody> message,
            CancellationToken? cancellation = null
        ) where TBody : class;
    }
}
