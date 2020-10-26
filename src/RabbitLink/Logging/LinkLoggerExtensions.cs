#region Usings

using System;

#endregion

namespace RabbitLink.Logging
{
    internal static class LinkLoggerExtensions
    {
        public static void Error(this ILinkLogger logger, string message)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Write(LinkLoggerLevel.Error, message);
        }

        public static void Warning(this ILinkLogger logger, string message)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Write(LinkLoggerLevel.Warning, message);
        }

        public static void Info(this ILinkLogger logger, string message)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Write(LinkLoggerLevel.Info, message);
        }

        public static void Debug(this ILinkLogger logger, string message)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Write(LinkLoggerLevel.Debug, message);
        }
    }
}
