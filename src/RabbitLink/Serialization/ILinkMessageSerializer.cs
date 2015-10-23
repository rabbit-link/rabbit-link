#region Usings

using System;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Serialization
{
    /// <summary>
    ///     Serializer for message serialization
    /// </summary>
    public interface ILinkMessageSerializer
    {
        /// <summary>
        ///     Serialize message
        /// </summary>
        /// <typeparam name="T">Message body <see cref="Type" /></typeparam>
        /// <param name="message">Message</param>
        /// <returns>Raw message</returns>
        ILinkMessage<byte[]> Serialize<T>(ILinkMessage<T> message) where T : class;

        /// <summary>
        ///     Deserialize messsage
        /// </summary>
        /// <typeparam name="T">Message body <see cref="Type" /></typeparam>
        /// <param name="message">Raw message</param>
        /// <returns>Deserialized message</returns>
        ILinkMessage<T> Deserialize<T>(ILinkMessage<byte[]> message) where T : class;
    }
}