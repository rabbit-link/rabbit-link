#region Usings

using System;
using System.Threading;
using RabbitLink.Consumer;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkConfiguration
    {
        private TimeSpan? _channelRecoveryInterval;
        private TimeSpan _connectionRecoveryInterval = TimeSpan.FromSeconds(10);
        private TimeSpan _connectionTimeout = TimeSpan.FromSeconds(10);        
        private TimeSpan? _consumerGetMessageTimeout;
        private ushort _consumerPrefetchCount = 1;
        private ILinkLoggerFactory _loggerFactory = new ActionLinkLoggerFactory(x => new LinkNullLogger());
        private ILinkMessageSerializer _messageSerializer = new JsonMessageSerializer();
        private LinkMessageProperties _producerMessageProperties = new LinkMessageProperties();
        private TimeSpan? _producerPublishTimeout;
        private TimeSpan? _topologyRecoveryInterval;
        private string _appId = Guid.NewGuid().ToString("D");
        private ILinkMessageIdGenerator _producerMessageIdGenerator = new LinkGuidMessageIdGenerator();

        public bool AutoStart { get; set; } = true;

        public string ConnectionString { get; set; }

        public TimeSpan ConnectionTimeout
        {
            get { return _connectionTimeout; }
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
            get { return _connectionRecoveryInterval; }
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
            get { return _channelRecoveryInterval ?? ConnectionRecoveryInterval; }
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
            get { return _topologyRecoveryInterval ?? ChannelRecoveryInterval; }
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
            get { return _loggerFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _loggerFactory = value;
            }
        }

        public LinkMessageProperties ProducerMessageProperties
        {
            get { return _producerMessageProperties; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _producerMessageProperties = value.Clone();
            }
        }

        public bool ProducerConfirmsMode { get; set; } = true;

        public TimeSpan? ProducerPublishTimeout
        {
            get { return _producerPublishTimeout; }
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
            get { return _consumerPrefetchCount; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than zero");

                _consumerPrefetchCount = value;
            }
        }

        public TimeSpan? ConsumerGetMessageTimeout
        {
            get { return _consumerGetMessageTimeout; }
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
            get { return _messageSerializer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _messageSerializer = value;
            }
        }

        public string AppId
        {
            get { return _appId; }
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
            get { return _producerMessageIdGenerator; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _producerMessageIdGenerator = value;
            }
        }
    }
}