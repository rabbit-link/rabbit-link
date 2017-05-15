#region Usings

using System;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkConfiguration
    {
        #region Fields

        private string _appId = Guid.NewGuid().ToString("D");
        private TimeSpan? _channelRecoveryInterval;
        private TimeSpan _connectionRecoveryInterval = TimeSpan.FromSeconds(10);
        private TimeSpan _connectionTimeout = TimeSpan.FromSeconds(10);
        private TimeSpan? _consumerGetMessageTimeout;
        private ushort _consumerPrefetchCount = 1;
        private ILinkLoggerFactory _loggerFactory = new LinkNullLoggingFactory();
        private ILinkMessageSerializer _messageSerializer = new JsonMessageSerializer();
        private ILinkMessageIdGenerator _producerMessageIdGenerator = new LinkGuidMessageIdGenerator();
        private LinkMessageProperties _producerMessageProperties = new LinkMessageProperties();
        private TimeSpan? _producerPublishTimeout;
        private TimeSpan? _topologyRecoveryInterval;

        #endregion

        #region Properties

        public bool AutoStart { get; set; } = true;

        public bool UseThreads { get; set; }

        public string ConnectionString { get; set; }

        public TimeSpan ConnectionTimeout
        {
            get => _connectionTimeout;
            set
            {
                if (
                    value.TotalMilliseconds <= 0 ||
                    value.TotalMilliseconds > int.MaxValue
                )
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "TotalMilliseconds must be greater than 0 and less than Int32.MaxValue");
                }

                _connectionTimeout = value;
            }
        }

        public TimeSpan ConnectionRecoveryInterval
        {
            get => _connectionRecoveryInterval;
            set
            {
                if (value.TotalMilliseconds <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "TotalMilliseconds must be greater than 0");
                }

                _connectionRecoveryInterval = value;
            }
        }

        public TimeSpan ChannelRecoveryInterval
        {
            get => _channelRecoveryInterval ?? ConnectionRecoveryInterval;
            set
            {
                if (value.TotalMilliseconds <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "TotalMilliseconds must be greater than 0");
                }

                _channelRecoveryInterval = value;
            }
        }

        public TimeSpan TopologyRecoveryInterval
        {
            get => _topologyRecoveryInterval ?? ChannelRecoveryInterval;
            set
            {
                if (value.TotalMilliseconds <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "TotalMilliseconds must be greater than 0");
                }

                _topologyRecoveryInterval = value;
            }
        }

        public ILinkLoggerFactory LoggerFactory
        {
            get => _loggerFactory;
            set => _loggerFactory = value ?? throw new ArgumentNullException(nameof(value));
        }

        public LinkMessageProperties ProducerMessageProperties
        {
            get => _producerMessageProperties;
            set => _producerMessageProperties = value?.Clone() ?? throw new ArgumentNullException(nameof(value));
        }

        public bool ProducerConfirmsMode { get; set; } = true;

        public TimeSpan? ProducerPublishTimeout
        {
            get => _producerPublishTimeout;
            set
            {
                if (value?.Ticks < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be positive or zero");

                _producerPublishTimeout = value;
            }
        }

        public bool ConsumerAutoAck { get; set; }

        public ushort ConsumerPrefetchCount
        {
            get => _consumerPrefetchCount;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than zero");

                _consumerPrefetchCount = value;
            }
        }

        public TimeSpan? ConsumerGetMessageTimeout
        {
            get => _consumerGetMessageTimeout;
            set
            {
                if (value?.Ticks < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be positive or zero");

                _consumerGetMessageTimeout = value;
            }
        }

        public bool ConsumerCancelOnHaFailover { get; set; }

        public ILinkMessageSerializer MessageSerializer
        {
            get => _messageSerializer;
            set => _messageSerializer = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string AppId
        {
            get => _appId;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(value));

                _appId = value;
            }
        }

        public bool ProducerSetUserId { get; set; }

        public ILinkMessageIdGenerator ProducerMessageIdGenerator
        {
            get => _producerMessageIdGenerator;
            set => _producerMessageIdGenerator = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion
    }
}