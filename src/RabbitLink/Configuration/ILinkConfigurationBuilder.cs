#region Usings

using System;
using RabbitLink.Consumer;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    public interface ILinkConfigurationBuilder
    {
        /// <summary>
        ///     Is connection must start automatically
        ///     By default true
        /// </summary>
        ILinkConfigurationBuilder AutoStart(bool value);

        /// <summary>
        ///     Connection timeout
        ///     By default 10 seconds
        /// </summary>
        ILinkConfigurationBuilder ConnectionTimeout(TimeSpan value);

        /// <summary>
        ///     Timeout before next connection attempt
        ///     By default 10 seconds
        /// </summary>
        ILinkConfigurationBuilder ConnectionRecoveryInterval(TimeSpan value);

        /// <summary>
        ///     Timeout before channel will be recovered when connection active
        ///     By default uses value from <see cref="ConnectionRecoveryInterval" />
        /// </summary>
        ILinkConfigurationBuilder ChannelRecoveryInterval(TimeSpan value);

        /// <summary>
        ///     Timeout before another try for topology configuration attempt
        ///     By default uses value from <see cref="ChannelRecoveryInterval" />
        /// </summary>
        ILinkConfigurationBuilder TopologyRecoveryInterval(TimeSpan value);

        /// <summary>
        ///     Logger factory
        ///     By default uses <see cref="LinkNullLogger" />
        /// </summary>
        ILinkConfigurationBuilder LoggerFactory(ILinkLoggerFactory value);

        /// <summary>
        ///     Confirms mode for producers (see https://www.rabbitmq.com/confirms.html)
        ///     By default true
        /// </summary>
        ILinkConfigurationBuilder ProducerConfirmsMode(bool value);

        /// <summary>
        ///     Base MessageProperties for producers
        /// </summary>
        ILinkConfigurationBuilder ProducerMessageProperties(LinkMessageProperties value);

        /// <summary>
        ///     Default publish timeout for producers
        ///     By default infinite
        /// </summary>
        ILinkConfigurationBuilder ProducerPublishTimeout(TimeSpan value);

        /// <summary>
        ///     Is need to force set <see cref="LinkMessageProperties.UserId" /> from connection string to all published messages
        ///     By default false
        /// </summary>
        ILinkConfigurationBuilder ProducerSetUserId(bool value);

        /// <summary>
        ///     Default consumer message prefetch count
        ///     By default 1
        /// </summary>
        ILinkConfigurationBuilder ConsumerPrefetchCount(ushort value);

        /// <summary>
        ///     Default consumer auto-ack mode
        ///     By default false
        /// </summary>
        ILinkConfigurationBuilder ConsumerAutoAck(bool value);

        /// <summary>
        ///     Default <see cref="ILinkConsumer.GetMessageAsync" /> timeout
        ///     By default infinite
        /// </summary>
        ILinkConfigurationBuilder ConsumerGetMessageTimeout(TimeSpan value);

        /// <summary>
        ///     Is consumers must be cancelled (then it will be automatically recover) on HA failover
        ///     See https://www.rabbitmq.com/ha.html for more details
        ///     By default false
        /// </summary>
        ILinkConfigurationBuilder ConsumerCancelOnHaFailover(bool value);

        /// <summary>
        ///     Default error strategy for consumers
        ///     By default <see cref="DefaultConsumerErrorStrategy" />
        /// </summary>
        /// <remarks>
        ///     Error strategy not working if <see cref="ConsumerAutoAck" /> is set
        /// </remarks>
        ILinkConfigurationBuilder ConsumerErrorStrategy(ILinkConsumerErrorStrategy value);

        /// <summary>
        ///     Default <see cref="ILinkMessageSerializer" /> for serialization / deserialization
        /// </summary>
        ILinkConfigurationBuilder MessageSerializer(ILinkMessageSerializer value);

        /// <summary>
        ///     Sets <see cref="LinkMessageProperties.AppId" /> to all published messages, white spaces will be trimmed, must be
        ///     not null or white space
        ///     By default Guit.NewValue().ToString("D")
        /// </summary>
        ILinkConfigurationBuilder AppId(string value);
    }
}