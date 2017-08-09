using System;
using RabbitLink.Connection;
using RabbitLink.Logging;

namespace RabbitLink.Builders
{
    internal struct LinkConfiguration
    {
        public LinkConfiguration(
            string connectionString,
            bool autoStart,
            TimeSpan timeout,
            TimeSpan recoveryInterval,
            ILinkLoggerFactory loggerFactory,
            string appId,
            LinkStateHandler<LinkConnectionState> stateHandler
        )
        {
            ConnectionString = connectionString;
            AutoStart = autoStart;
            Timeout = timeout;
            RecoveryInterval = recoveryInterval;
            LoggerFactory = loggerFactory;
            AppId = appId;
            StateHandler = stateHandler;
        }
        
        public string ConnectionString { get; }
        public bool AutoStart { get; }
        public TimeSpan Timeout { get; }
        public TimeSpan RecoveryInterval { get; }
        public ILinkLoggerFactory LoggerFactory { get; }
        public string AppId { get; }
        public LinkStateHandler<LinkConnectionState> StateHandler { get; }
    }
}