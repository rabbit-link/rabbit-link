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
            if(string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            
            if(timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Must be greater than TimeSpan.Zero");
            
            if(recoveryInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(recoveryInterval), "Must be greater than TimeSpan.Zero");
            
            if(string.IsNullOrWhiteSpace(appId))
                throw new ArgumentNullException(nameof(appId));
            
            ConnectionString = connectionString.Trim();
            AutoStart = autoStart;
            Timeout = timeout;
            RecoveryInterval = recoveryInterval;
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            AppId = appId.Trim();
            StateHandler = stateHandler ?? throw new ArgumentNullException(nameof(stateHandler));
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