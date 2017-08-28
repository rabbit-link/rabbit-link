#region Usings

using System;

#endregion

namespace RabbitLink.Exceptions
{
    /// <summary>
    /// Base class for all exceptions in library
    /// </summary>
    public abstract class LinkException : Exception
    {
        /// <summary>
        /// Construct instance with message
        /// </summary>
        /// <param name="message">message</param>
        protected LinkException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructs instance with message and inner exception
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="innerException">inner exception</param>
        protected LinkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}