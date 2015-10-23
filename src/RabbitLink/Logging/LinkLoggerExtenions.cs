﻿#region Usings

using System;

#endregion

namespace RabbitLink.Logging
{
    public static class LinkLoggerExtenions
    {
        public static void Error(this ILinkLogger logger, string format, params object[] args)
        {
            logger.Error(string.Format(format, args));
        }

        public static void Error(this ILinkLogger logger, string message)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Write(LinkLoggerLevel.Error, message);
        }

        public static void Warning(this ILinkLogger logger, string format, params object[] args)
        {
            logger.Warning(string.Format(format, args));
        }

        public static void Warning(this ILinkLogger logger, string message)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Write(LinkLoggerLevel.Warning, message);
        }

        public static void Info(this ILinkLogger logger, string format, params object[] args)
        {
            logger.Info(string.Format(format, args));
        }

        public static void Info(this ILinkLogger logger, string message)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Write(LinkLoggerLevel.Info, message);
        }

        public static void Debug(this ILinkLogger logger, string format, params object[] args)
        {
            logger.Debug(string.Format(format, args));
        }

        public static void Debug(this ILinkLogger logger, string message)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Write(LinkLoggerLevel.Debug, message);
        }
    }
}