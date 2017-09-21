#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Connection
{
    class LinkConnectionFactory : ILinkConnectionFactory
    {
        #region Fields

        private readonly ConnectionFactory _factory;

        private readonly List<string> _hostNames;

        #endregion

        #region Ctor

        public LinkConnectionFactory(
            string name,
            string appId,
            Uri connectionString,
            TimeSpan timeout,
            bool useBackgroundThreads
        )
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (timeout.TotalMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout.TotalMilliseconds,
                    "Must be grater than 0 milliseconds");

            Name = name;

            _factory = new ConnectionFactory
            {
                Uri = connectionString ?? throw new ArgumentNullException(nameof(connectionString)),
                TopologyRecoveryEnabled = false,
                AutomaticRecoveryEnabled = false,
                UseBackgroundThreadsForIO = useBackgroundThreads,
                RequestedConnectionTimeout = (int) timeout.TotalMilliseconds,
                ClientProperties =
                {
                    ["product"] = "RabbitLink",
                    ["version"] = GetType().GetTypeInfo().Assembly.GetName().Version.ToString(3),
                    ["copyright"] = "Copyright (c) 2015-2017 RabbitLink",
                    ["information"] = "https://github.com/rabbit-link/rabbit-link",
                    ["app_id"] = appId
                }
            };

            _hostNames = new List<string> {_factory.HostName};
        }

        #endregion

        #region Properties

        public IReadOnlyCollection<string> HostNames => _hostNames;

        #endregion

        #region ILinkConnectionFactory Members

        public string Name { get; }
        public string UserName => _factory.UserName;

        public IConnection GetConnection()
        {
            return _factory.CreateConnection(_hostNames, Name);
        }

        #endregion
    }
}
