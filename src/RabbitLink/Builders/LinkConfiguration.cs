using System;
using RabbitLink.Logging;

namespace RabbitLink.Builders
{
    internal struct LinkConfiguration
    {
        public bool AutoStart;
        public string ConnectionString;
        public TimeSpan Timeout;
        public TimeSpan RecoveryInterval;
        public ILinkLoggerFactory LoggerFactory;
        public string AppId;
    }
}