#region Usings

using System;
using RabbitLink.Connection;
using RabbitLink.Logging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkBuilder : ILinkBuilder
    {
        private readonly string _connectionName;
        private readonly Uri _connectionString;
        private readonly bool _autoStart;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan? _recoveryInterval;
        private readonly ILinkLoggerFactory _loggerFactory;
        private readonly string _appId;
        private readonly LinkStateHandler<LinkConnectionState> _stateHandler;
        private readonly bool _useBackgoundsThreadsForConnection;
        private readonly ILinkSerializer _serializer;

        public LinkBuilder(
            string connectionName = null,
            Uri connectionString = null,
            bool? autoStart = null,
            TimeSpan? timeout = null,
            TimeSpan? recoveryInterval = null,
            ILinkLoggerFactory loggerFactory = null,
            string appId = null,
            LinkStateHandler<LinkConnectionState> stateHandler = null,
            bool? useBackgroundThreadsForConnection = null,
            ILinkSerializer serializer = null
        )
        {
            _connectionName = connectionName ?? "default";
            _connectionString = connectionString;
            _autoStart = autoStart ?? true;
            _timeout = timeout ?? TimeSpan.FromSeconds(10);
            _recoveryInterval = recoveryInterval;
            _loggerFactory = loggerFactory ?? new LinkNullLoggingFactory();
            _appId = appId ?? Guid.NewGuid().ToString("D");
            _stateHandler = stateHandler ?? ((old, @new) => { });
            _useBackgoundsThreadsForConnection = useBackgroundThreadsForConnection ?? false;
            _serializer = serializer;
        }

        private LinkBuilder(
            LinkBuilder prev,
            string connectionName = null,
            Uri connectionString = null,
            bool? autoStart = null,
            TimeSpan? timeout = null,
            TimeSpan? recoveryInterval = null,
            ILinkLoggerFactory loggerFactory = null,
            string appId = null,
            LinkStateHandler<LinkConnectionState> stateHandler = null,
            bool? useBackgroundThreadsForConnection = null,
            ILinkSerializer serializer = null
        ) : this(
            connectionName ?? prev._connectionName,
            connectionString ?? prev._connectionString,
            autoStart ?? prev._autoStart,
            timeout ?? prev._timeout,
            recoveryInterval ?? prev._recoveryInterval,
            loggerFactory ?? prev._loggerFactory,
            appId ?? prev._appId,
            stateHandler ?? prev._stateHandler,
            useBackgroundThreadsForConnection ?? prev._useBackgoundsThreadsForConnection,
            serializer ?? prev._serializer
        )
        {
        }

        public ILinkBuilder ConnectionName(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));
            
            return  new LinkBuilder(this, connectionName: value.Trim());
        }

        public ILinkBuilder Uri(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            return Uri(new Uri(value));
        }

        public ILinkBuilder Uri(Uri value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkBuilder(this, connectionString: value);
        }

        public ILinkBuilder AutoStart(bool value)
        {
            return new LinkBuilder(this, autoStart: value);
        }

        public ILinkBuilder Timeout(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than TimeSpan.Zero");

            return new LinkBuilder(this, timeout: value);
        }

        public ILinkBuilder RecoveryInterval(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than TimeSpan.Zero");

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

        public ILinkBuilder OnStateChange(LinkStateHandler<LinkConnectionState> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkBuilder(this, stateHandler: handler);
        }

        public ILinkBuilder UseBackgroundThreadsForConnection(bool value)
        {
            return new LinkBuilder(this, useBackgroundThreadsForConnection: value);
        }

        public ILinkBuilder Serializer(ILinkSerializer value)
        {
            if(value == null)
                throw new ArgumentNullException(nameof(value));
            
            return new LinkBuilder(this, serializer: value);
        }

        public ILink Build()
        {
            var config = new LinkConfiguration(
                _connectionName ?? throw new InvalidOperationException($"{nameof(Uri)} must be set"),
                _connectionString,
                _autoStart,
                _timeout,
                _recoveryInterval ?? _timeout,
                _loggerFactory,
                _appId,
                _stateHandler,
                _useBackgoundsThreadsForConnection,
                _serializer
            );

            return new Link(config);
        }
    }
}