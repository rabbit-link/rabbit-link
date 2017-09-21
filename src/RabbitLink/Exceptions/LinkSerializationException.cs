using System;

namespace RabbitLink.Exceptions
{
    /// <summary>
    /// Fires when message cannot be serialized
    /// </summary>
    public class LinkSerializationException : LinkException
    {
        /// <summary>
        ///     Construct instance
        /// </summary>
        public LinkSerializationException(Exception innerException)
            : base("Cannot serialize message, see InnerException for details", innerException)
        {
        }
    }
}
