﻿#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Messaging;
using RabbitLink.Rpc;

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
        /// <returns><see cref="Task" /> which completed when message acked by brocker</returns>
        Task PublishAsync(
            ILinkPublishMessage<byte[]> message,
            CancellationToken? cancellation = null
        );

        /// <summary>
        /// Serialize message body and publishes message
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="cancellation">cancellation token, if null <see cref="Timeout" /> will be used</param>
        /// <returns><see cref="Task" /> which completed when message acked by brocker</returns>
        Task PublishAsync<TBody>(
            ILinkPublishMessage<TBody> message,
            CancellationToken? cancellation = null
        ) where TBody : class;

        /// <summary>
        /// Untyped rpc call
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="replayConsumer">consumer to wait replay with</param>
        /// <param name="cancellation">cancellation token, if null <see cref="Timeout" /> will be used</param>
        /// <returns><see cref="Task" /> which completed when response will received</returns>
        Task<ILinkConsumedMessage<byte[]>> CallAsync(ILinkPublishMessage<byte[]> message,
            LinkReplayConsumer replayConsumer,
            CancellationToken? cancellation = null);

        /// <summary>
        /// Typed rpc call
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="replayConsumer">consumer to wait replay with</param>
        /// <param name="cancellation">cancellation token, if null <see cref="Timeout" /> will be used</param>
        /// <returns><see cref="Task" /> which completed when response will received</returns>
        Task<ILinkConsumedMessage<TResponse>> CallAsync<TRequest, TResponse>(ILinkPublishMessage<TRequest> message,
            LinkReplayConsumer replayConsumer,
            CancellationToken? cancellation = null)
            where TRequest : class
            where TResponse : class;
    }
}
