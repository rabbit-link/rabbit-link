using System;
using RabbitLink.Messaging;

namespace RabbitLink.Serialization
{
    /// <summary>
    /// Message serializer
    /// </summary>
    public interface ILinkSerializer
    {
        /// <summary>
        ///     Serialize message body and set properties
        /// </summary>
        /// <typeparam name="TBody">Message body <see cref="Type" /></typeparam>        
        /// <returns>Raw message</returns>
        byte[] Serialize<TBody>(TBody body, LinkMessageProperties properties) where TBody : class;

        /// <summary>
        ///     Deserialize messsage and set properties
        /// </summary>
        /// <typeparam name="TBody">Message body <see cref="Type" /></typeparam>        
        /// <returns>Deserialized message</returns>
        TBody Deserialize<TBody>(byte[] body, LinkMessageProperties properties) where TBody : class;
    }
}
