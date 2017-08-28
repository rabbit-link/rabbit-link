#region Usings

using RabbitLink.Consumer;
using System;

#endregion

namespace RabbitLink.Builders
{
    /// <summary>
    /// Builder for <see cref="ILinkConsumer"/>
    /// </summary>
    public interface ILinkConsumerBuilder
    {
        /// <summary>
        ///     Message prefetch count
        ///     By default use <see cref="ILinkBuilder.ConsumerPrefetchCount" /> value
        /// </summary>
        ILinkConsumerBuilder PrefetchCount(ushort value);

        /// <summary>
        ///     Auto ack on consume
        ///     By default use <see cref="ILinkBuilder.ConsumerAutoAck" /> value
        /// </summary>
        ILinkConsumerBuilder AutoAck(bool value);

        /// <summary>
        ///     Consumer priority
        ///     See https://www.rabbitmq.com/consumer-priority.html for more details
        ///     By Default 0
        /// </summary>
        ILinkConsumerBuilder Priority(int value);

        /// <summary>
        ///     Is consumer must be cancelled (then it will be automatically recover) on HA failover
        ///     See https://www.rabbitmq.com/ha.html for more details
        ///     By default <see cref="ILinkBuilder.ConsumerCancelOnHaFailover" /> value
        /// </summary>
        ILinkConsumerBuilder CancelOnHaFailover(bool value);

        /// <summary>
        ///     Is consumer must be exclusive
        ///     By default false
        /// </summary>
        ILinkConsumerBuilder Exclusive(bool value);

        /// <summary>
        /// Error strategy
        /// </summary>
        ILinkConsumerBuilder ErrorStrategy(ILinkConsumerErrorStrategy value);

        /// <summary>
        /// Message handler
        /// </summary>
        ILinkConsumerBuilder Handler(LinkConsumerMessageHandlerDelegate value);
    }
}