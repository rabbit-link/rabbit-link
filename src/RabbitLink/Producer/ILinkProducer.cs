#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Producer
{
    public interface ILinkProducer : IDisposable
    {
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
        ///     Publishes message
        /// </summary>
        /// <param name="message">
        ///     message to publish, it will be serialized by <see cref="ILinkMessageSerializer" /> if needed
        /// </param>
        /// <typeparam name="T">
        ///     If byte[] will send raw message
        ///     Else send serialized message
        ///     If TypeMap contains map for this type then adds Type header
        /// </typeparam>
        /// <param name="properties">publication properties</param>
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