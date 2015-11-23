#region Usings

using System;
using RabbitLink.Consumer;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkConfigurationBuilder : ILinkConfigurationBuilder
    {
        internal LinkConfiguration Configuration { get; } = new LinkConfiguration();

        public ILinkConfigurationBuilder AutoStart(bool value)
        {
            Configuration.AutoStart = value;
            return this;
        }

        public ILinkConfigurationBuilder ConnectionTimeot(TimeSpan value)
        {
            Configuration.ConnectionTimeout = value;
            return this;
        }

        public ILinkConfigurationBuilder ConnectionRecoveryInterval(TimeSpan value)
        {
            Configuration.ConnectionRecoveryInterval = value;
            return this;
        }

        public ILinkConfigurationBuilder ChannelRecoveryInterval(TimeSpan value)
        {
            Configuration.ChannelRecoveryInterval = value;
            return this;
        }

        public ILinkConfigurationBuilder TopologyRecoveryInterval(TimeSpan value)
        {
            Configuration.TopologyRecoveryInterval = value;
            return this;
        }

        public ILinkConfigurationBuilder LoggerFactory(ILinkLoggerFactory value)
        {
            Configuration.LoggerFactory = value;
            return this;
        }

        public ILinkConfigurationBuilder ProducerConfirmsMode(bool value)
        {
            Configuration.ProducerConfirmsMode = value;
            return this;
        }

        public ILinkConfigurationBuilder ProducerMessageProperties(LinkMessageProperties value)
        {
            Configuration.ProducerMessageProperties = value;
            return this;
        }

        public ILinkConfigurationBuilder ProducerPublishTimeout(TimeSpan value)
        {
            Configuration.ProducerPublishTimeout = value;
            return this;
        }

        public ILinkConfigurationBuilder ProducerSetUserId(bool value)
        {
            Configuration.SetUserId = value;
            return this;
        }

        public ILinkConfigurationBuilder ConsumerPrefetchCount(ushort value)
        {
            Configuration.ConsumerPrefetchCount = value;
            return this;
        }

        public ILinkConfigurationBuilder ConsumerAutoAck(bool value)
        {
            Configuration.ConsumerAutoAck = value;
            return this;
        }

        public ILinkConfigurationBuilder ConsumerGetMessageTimeout(TimeSpan value)
        {
            Configuration.ConsumerGetMessageTimeout = value;
            return this;
        }

        public ILinkConfigurationBuilder ConsumerCancelOnHaFailover(bool value)
        {
            Configuration.ConsumerCancelOnHaFailover = value;
            return this;
        }

        public ILinkConfigurationBuilder ConsumerErrorStrategy(ILinkConsumerErrorStrategy value)
        {
            Configuration.ConsumerErrorStrategy = value;
            return this;
        }

        public ILinkConfigurationBuilder MessageSerializer(ILinkMessageSerializer value)
        {
            Configuration.MessageSerializer = value;
            return this;
        }

        public ILinkConfigurationBuilder AppId(string value)
        {
            Configuration.AppId = value;
            return this;
        }
    }
}