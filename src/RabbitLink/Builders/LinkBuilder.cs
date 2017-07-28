#region Usings

using System;
using RabbitLink.Logging;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkBuilder : ILinkBuilder
    {
        private readonly string _connectionString;
        private readonly bool _autoStart;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan? _recoveryInterval;
        private readonly ILinkLoggerFactory _loggerFactory;
        private readonly string _appId;
        

        public LinkBuilder(
            string connectionString = null, 
            bool? autoStart = null, 
            TimeSpan? timeout = null, 
            TimeSpan? recoveryInterval = null, 
            ILinkLoggerFactory loggerFactory = null, 
            string appId = null
        )
        {
            _connectionString = connectionString;
            _autoStart = autoStart ?? true;
            _timeout = timeout ?? TimeSpan.FromSeconds(10);
            _recoveryInterval = recoveryInterval;
            _loggerFactory = loggerFactory ?? new LinkNullLoggingFactory();
            _appId = appId ?? Guid.NewGuid().ToString("D");
        }
        
        private LinkBuilder(
            LinkBuilder prev,
            string connectionString = null, 
            bool? autoStart = null, 
            TimeSpan? timeout = null, 
            TimeSpan? recoveryInterval = null, 
            ILinkLoggerFactory loggerFactory = null, 
            string appId = null
        ) : this (
            connectionString ?? prev._connectionString,
            autoStart ?? prev._autoStart,
            timeout ?? prev._timeout,
            recoveryInterval ?? prev._recoveryInterval,
            loggerFactory ?? prev._loggerFactory,
            appId ?? prev._appId
        )
        {
            
        }

        public ILinkBuilder ConnectionString(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            return new LinkBuilder(this, connectionString: value.Trim());
        }

        public ILinkBuilder AutoStart(bool value)
        {
            return new LinkBuilder(this, autoStart: value);
        }

        public ILinkBuilder Timeout(TimeSpan value)
        {
            if (
                value.TotalMilliseconds <= 0 ||
                value.TotalMilliseconds > int.MaxValue
            )
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    "TotalMilliseconds must be greater than 0 and less than Int32.MaxValue");
            }

            return new LinkBuilder(this, timeout: value);
        }

        public ILinkBuilder RecoveryInterval(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than zero");

            return new LinkBuilder(this, recoveryInterval: value);
        }

        public ILinkBuilder LoggerFactory(ILinkLoggerFactory value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkBuilder(this, loggerFactory: value);
        }

        public ILinkBuilder AppId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            return new LinkBuilder(this, appId: value.Trim());
        }

        public ILink Build()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException($"{nameof(ConnectionString)} must be set");
            
            var config = new LinkConfiguration
            {
                AppId = _appId,
                AutoStart = _autoStart,
                ConnectionString = _connectionString,
                LoggerFactory = _loggerFactory,
                Timeout = _timeout,
                RecoveryInterval = _recoveryInterval ?? _timeout
            };

            return new Link(config);
        }
    }
}