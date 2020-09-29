using System;
using Microsoft.Extensions.Logging;

namespace RabbitLink.Logging
{
    internal class Logger : ILinkLogger
    {
        private readonly ILogger _logger;

        public Logger(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            
        }

        public void Write(LinkLoggerLevel level, string message)
        {
            switch (level)
            {
                case LinkLoggerLevel.Error:
                    _logger.LogError(message);
                    break;
                case LinkLoggerLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case LinkLoggerLevel.Info:
                    _logger.LogInformation(message);
                    break;
                case LinkLoggerLevel.Debug:
                    _logger.LogDebug(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}
