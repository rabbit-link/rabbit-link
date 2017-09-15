using System;
using RabbitLink.Connection;
using RabbitLink.Logging;

namespace RabbitLink.Builders
{
    internal struct LinkConfiguration
    {
        public LinkConfiguration(
            string connectionName,
            Uri connectionString,
            bool autoStart,
            TimeSpan timeout,
            TimeSpan recoveryInterval,
            ILinkLoggerFactory loggerFactory,
            string appId,
            LinkStateHandler<LinkConnectionState> stateHandler,
            bool useBackgroundThreadsForConnection
        )
        {
            if(string.IsNullOrWhiteSpace(connectionName))
                throw new ArgumentNullException(nameof(connectionName));
            
            if(timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Must be greater than TimeSpan.Zero");
            
            if(recoveryInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(recoveryInterval), "Must be greater than TimeSpan.Zero");
            
            if(string.IsNullOrWhiteSpace(appId))
                throw new ArgumentNullException(nameof(appId));

            ConnectionName = connectionName.Trim();
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            AutoStart = autoStart;
            Timeout = timeout;
            RecoveryInterval = recoveryInterval;
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            AppId = appId.Trim();
            StateHandler = stateHandler ?? throw new ArgumentNullException(nameof(stateHandler));
            UseBackgroundThreadsForConnection = useBackgroundThreadsForConnection;
        }
        
        public string ConnectionName { get; }
        public Uri ConnectionString { get; }
        public bool AutoStart { get; }
        public TimeSpan Timeout { get; }
        public TimeSpan RecoveryInterval { get; }
        public ILinkLoggerFactory LoggerFactory { get; }
        public string AppId { get; }
        public LinkStateHandler<LinkConnectionState> StateHandler { get; }
        public bool UseBackgroundThreadsForConnection { get; }
    }
}