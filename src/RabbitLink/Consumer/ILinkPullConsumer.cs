#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    public interface ILinkPullConsumer : ILinkConsumer
    {
        /// <summary>
        ///     <see cref="GetMessageAsync" /> operation timeout
        /// </summary>
        TimeSpan? GetMessageTimeout { get; }

        /// <summary>
        ///     Wait for message to be recieved,
        ///     then map with TypeNameMapping and deserialize body if mapping successfull
        ///     if mapping not successfull returns <see cref="ILinkAckableRecievedMessage{byte[]}" />
        /// </summary>
        /// <param name="cancellation">
        ///     cancellation, if null <see cref="GetMessageTimeout" />
        ///     value will be used
        /// </param>
        /// <returns>Deserialied message</returns>
        /// <exception cref="LinkDeserializationException">On serialization error, which </exception>
        Task<ILinkAckableRecievedMessage<object>> GetMessageAsync(CancellationToken? cancellation = null);


        /// <summary>
        ///     Wait for message to be recieved,
        ///     then deserialize it body to type T.
        ///     If you need to get raw message just set T = byte[]
        /// </summary>
        /// <param name="cancellation">
        ///     cancellation, if null <see cref="GetMessageTimeout" />
        ///     value will be used
        /// </param>
        /// <returns>Deserialied message</returns>
        /// <exception cref="LinkDeserializationException">On serialization error</exception>
        Task<ILinkAckableRecievedMessage<T>> GetMessageAsync<T>(CancellationToken? cancellation = null)
            where T : class;
    }
}