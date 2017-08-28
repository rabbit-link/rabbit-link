#region Usings

using System;

#endregion

namespace RabbitLink.Exceptions
{
    /// <summary>
    ///     Base class for message publish exceptions
    /// </summary>
    public abstract class LinkMessagePublishException : LinkException
    {
        #region Ctor

        /// <summary>
        ///     Constructs instance with message
        /// </summary>
        /// <param name="message">message</param>
        protected LinkMessagePublishException(string message) : base(message)
        {
        }

        /// <summary>
        ///     Constructs instance with message and inner exception
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="innerException">inner exception</param>
        protected LinkMessagePublishException(string message, Exception innerException) : base(message, innerException)
        {
        }

        #endregion
    }
}