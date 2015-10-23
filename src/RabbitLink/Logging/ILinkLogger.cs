#region Usings

using System;

#endregion

namespace RabbitLink.Logging
{
    /// <summary>
    ///     Logger interface for <see cref="Link" /> and underlying components
    /// </summary>
    public interface ILinkLogger : IDisposable
    {
        /// <summary>
        ///     Writes message to log
        /// </summary>
        /// <param name="level">Error level</param>
        /// <param name="message">Message to write</param>
        void Write(LinkLoggerLevel level, string message);
    }
}