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
        /// <returns>Raw message</returns>
        byte[] Serialize<T>(T body, LinkMessageProperties properties) where T : class;

        /// <summary>
        ///     Deserialize messsage
        /// </summary>
        /// <typeparam name="T">Message body <see cref="Type" /></typeparam>        
        /// <returns>Deserialized message</returns>
        T Deserialize<T>(byte[] body, LinkMessageProperties properties) where T : class;
    }
}