#region Usings

using System;
using RabbitLink.Internals;

#endregion

namespace RabbitLink.Logging
{
    /// <summary>
    ///     Implements <see cref="ILinkLogger" /> and wraps exiting <see cref="ILinkLogger" /> implementation with buffered
    ///     output.
    ///     When disposing it also dispose underlyingLogger
    /// </summary>
    public class BufferedLinkLogger : ILinkLogger
    {
        private readonly EventLoop _eventLoop;

        /// <summary>
        ///     Creates new instance of <see cref="BufferedLinkLogger" />
        /// </summary>
        /// <param name="undelyingLogger">Underlying logger to write messages</param>
        /// <param name="maxMessages">Maximum number of messages in buffer, after buffer overflow it will blocks next writes</param>
        public BufferedLinkLogger(ILinkLogger undelyingLogger, int? maxMessages = 1000)
        {
            if (undelyingLogger == null)
                throw new ArgumentNullException(nameof(undelyingLogger));
            if (maxMessages <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(maxMessages));

            UnderlyingLogger = undelyingLogger;

            _eventLoop = maxMessages == null
                ? new EventLoop(EventLoop.DisposingStrategy.Wait)
                : new EventLoop(maxMessages.Value, EventLoop.DisposingStrategy.Wait);
        }

        public ILinkLogger UnderlyingLogger { get; }

        public void Dispose()
        {
            _eventLoop.Dispose();
            UnderlyingLogger.Dispose();
        }

        public void Write(LinkLoggerLevel level, string message)
        {
            _eventLoop.ScheduleAsync(() => { UnderlyingLogger.Write(level, message); });
        }
    }
}