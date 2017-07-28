#region Usings

using System;

#endregion

namespace RabbitLink.Builders
{
    public interface ILinkConsumerConfigurationBuilder
    {
        /// <summary>
        ///     Message prefetch count
        ///     By default use <see cref="ILinkBuilder.ConsumerPrefetchCount" /> value
        /// </summary>
        ILinkConsumerConfigurationBuilder PrefetchCount(ushort value);

        /// <summary>
        ///     Auto ack on consume
        ///     By default use <see cref="ILinkBuilder.ConsumerAutoAck" /> value
        /// </summary>
        ILinkConsumerConfigurationBuilder AutoAck(bool value);

        /// <summary>
        ///     Consumer priority
        ///     See https://www.rabbitmq.com/consumer-priority.html for more details
        ///     By Default 0
        /// </summary>
        ILinkConsumerConfigurationBuilder Priority(int value);

        /// <summary>
        ///     Is consumer must be cancelled (then it will be automatically recover) on HA failover
        ///     See https://www.rabbitmq.com/ha.html for more details
        ///     By default <see cref="ILinkBuilder.ConsumerCancelOnHaFailover" /> value
        /// </summary>
        ILinkConsumerConfigurationBuilder CancelOnHaFailover(bool value);

        /// <summary>
        ///     Is consumer must be exclusive
        ///     By default false
        /// </summary>
        ILinkConsumerConfigurationBuilder Exclusive(bool value);

        /// <summary>
        ///     GetMessageAsync operation timeout
        ///     By default <see cref="ILinkBuilder.ConsumerGetMessageTimeout" /> value
        ///     null = infinite
        /// </summary>
        ILinkConsumerConfigurationBuilder GetMessageTimeout(TimeSpan? value);
    }
}