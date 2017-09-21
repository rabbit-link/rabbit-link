#region Usings

using System;

#endregion

namespace RabbitLink.Exceptions
{
    /// <summary>
    ///     Fires when operation not supported
    /// </summary>
    public class LinkNotSupportedException : LinkException
    {
        #region Ctor

        /// <summary>
        ///     Construct instance with message
        /// </summary>
        /// <param name="message">message</param>
        public LinkNotSupportedException(string message) : base(message)
        {
        }

        /// <summary>
        ///     Constructs instance with message and inner exception
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="innerException">inner exception</param>
        public LinkNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        #endregion
    }
}
