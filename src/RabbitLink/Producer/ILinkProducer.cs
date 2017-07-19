#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Producer
{
    /// <summary>
    ///     Represents RabbitMQ message producer
    /// </summary>
    public interface ILinkProducer : IDisposable
    {
        #region Properties

        /// <summary>
        ///     Producer Id
        /// </summary>
        Guid Id { get; }

        /// <summary>
        ///     Confirm mode (see https://www.rabbitmq.com/confirms.html)
        /// </summary>
        bool ConfirmsMode { get; }

        /// <summary>
        ///     Base publish properties
        /// </summary>
        LinkPublishProperties PublishProperties { get; }

        /// <summary>
        ///     Base message properties
        /// </summary>
        LinkMessageProperties MessageProperties { get; }

        /// <summary>
        ///     Publish operation default timeout
        /// </summary>
        TimeSpan? PublishTimeout { get; }

        /// <summary>
        ///     Orational state
        /// </summary>
        LinkProducerState State { get; }

        #endregion

        /// <summary>
        ///     Publishes message
        /// </summary>
        /// <typeparam name="T">
        ///     If byte[] will send raw message
        ///     Else send serialized message
        ///     If TypeMap contains map for this type then adds Type header
        /// </typeparam>
        /// <param name="body">message body</param>
        /// <param name="properties">message properties</param>
        /// <param name="publishProperties">publication properties</param>
        /// <param name="cancellation">cancellation token, if null <see cref="Timeout" /> will be used</param>
        /// <returns><see cref="Task" /> which completed when message acked by brocker</returns>
        /// <exception cref="LinkSerializationException">On serialization error</exception>
        Task PublishAsync<T>(
            T body,
            LinkMessageProperties properties = null,
            LinkPublishProperties publishProperties = null,
            CancellationToken? cancellation = null
        ) where T : class;
    }
}