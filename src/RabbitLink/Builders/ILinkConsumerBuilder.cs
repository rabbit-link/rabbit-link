#region Usings

using RabbitLink.Consumer;
using System;
using RabbitLink.Connection;
using RabbitLink.Topology;

#endregion

namespace RabbitLink.Builders
{
    /// <summary>
    /// Builder for <see cref="ILinkConsumer"/>
    /// </summary>
    public interface ILinkConsumerBuilder
    {
        /// <summary>
        /// Builds instance of <see cref="ILinkConsumer"/>
        /// </summary>
        ILinkConsumer Build();

        /// <summary>
        /// Channel / Topology recovery interval
        /// By default <see cref="ILinkBuilder.RecoveryInterval"/>
        /// </summary>
        ILinkConsumerBuilder RecoveryInterval(TimeSpan value);

        /// <summary>
        ///     Message prefetch count
        ///     By default 0 = no limit
        /// </summary>
        ILinkConsumerBuilder PrefetchCount(ushort value);

        /// <summary>
        ///     Auto ack on consume
        ///     By default false
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

        /// <summary>
        /// Sets handler for state changes
        /// </summary>
        ILinkConsumerBuilder OnStateChange(LinkStateHandler<LinkConsumerState> value);

        /// <summary>
        /// Sets handler for channel state changes
        /// </summary>
        ILinkConsumerBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> value);

        /// <summary>
        /// Sets topology handler for queue
        /// </summary>
        ILinkConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config);

        /// <summary>
        /// Sets topology handler for queue and topology exception handler
        /// </summary>
        ILinkConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config, LinkTopologyErrorDelegate error);

        /// <summary>
        ///  Sets topology handler
        /// </summary>
        ILinkConsumerBuilder Queue(ILinkConsumerTopologyHandler handler);
    }
}