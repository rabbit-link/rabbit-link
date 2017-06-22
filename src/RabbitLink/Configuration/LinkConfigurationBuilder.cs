#region Usings

using System;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkConfigurationBuilder : ILinkConfigurationBuilder
    {
        #region Properties

        internal LinkConfiguration Configuration { get; } = new LinkConfiguration();

        #endregion

        #region ILinkConfigurationBuilder Members

        public ILinkConfigurationBuilder AutoStart(bool value)
        {
            Configuration.AutoStart = value;
            return this;
        }

        public ILinkConfigurationBuilder ConnectionTimeout(TimeSpan value)
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
            Configuration.ProducerSetUserId = value;
            return this;
        }

        public ILinkConfigurationBuilder ProducerMessageIdGenerator(ILinkMessageIdGenerator value)
        {
            Configuration.ProducerMessageIdGenerator = value;
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

        #endregion
    }
}