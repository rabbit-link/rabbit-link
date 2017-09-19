using System;
using System.Collections.Generic;
using System.Threading;
using RabbitLink.Connection;
using RabbitLink.Consumer;
using RabbitLink.Serialization;
using RabbitLink.Topology;

namespace RabbitLink.Builders
{
    /// <summary>
    /// Builder for <see cref="ILinkPullConsumer"/>
    /// </summary>
    public interface ILinkPullConsumerBuilder
    {
        /// <summary>
        /// Builds instance of <see cref="ILinkConsumer"/>
        /// </summary>
        ILinkPullConsumer Build();

        /// <summary>
        /// Channel / Topology recovery interval
        /// By default <see cref="ILinkBuilder.RecoveryInterval"/>
        /// </summary>
        ILinkPullConsumerBuilder RecoveryInterval(TimeSpan value);

        /// <summary>
        ///     Message prefetch count
        ///     By default 0 = no limit
        /// </summary>
        ILinkPullConsumerBuilder PrefetchCount(ushort value);

        /// <summary>
        ///     Auto ack on consume
        ///     By default false
        /// </summary>
        ILinkPullConsumerBuilder AutoAck(bool value);

        /// <summary>
        ///     Consumer priority
        ///     See https://www.rabbitmq.com/consumer-priority.html for more details
        ///     By Default 0
        /// </summary>
        ILinkPullConsumerBuilder Priority(int value);

        /// <summary>
        ///     Is consumer must be cancelled (then it will be automatically recover) on HA failover
        ///     See https://www.rabbitmq.com/ha.html for more details
        /// </summary>
        ILinkPullConsumerBuilder CancelOnHaFailover(bool value);

        /// <summary>
        ///     Is consumer must be exclusive
        ///     By default false
        /// </summary>
        ILinkPullConsumerBuilder Exclusive(bool value);

        /// <summary>
        /// Sets handler for state changes
        /// </summary>
        ILinkPullConsumerBuilder OnStateChange(LinkStateHandler<LinkConsumerState> value);

        /// <summary>
        /// Sets handler for channel state changes
        /// </summary>
        ILinkPullConsumerBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> value);

        /// <summary>
        /// Sets topology handler for queue
        /// </summary>
        ILinkPullConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config);

        /// <summary>
        /// Sets topology handler for queue and topology exception handler
        /// </summary>
        ILinkPullConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config, LinkTopologyErrorDelegate error);

        /// <summary>
        ///  Sets topology handler
        /// </summary>
        ILinkPullConsumerBuilder Queue(ILinkConsumerTopologyHandler handler);

        /// <summary>
        /// Timeout <see cref="ILinkPullConsumer.GetMessageAsync"/>
        /// By default <see cref="Timeout.InfiniteTimeSpan"/>
        /// </summary>
        ///<param name="value">Use <see cref="Timeout.InfiniteTimeSpan"/> or <see cref="TimeSpan.Zero"/> for infinite</param>
        ILinkPullConsumerBuilder GetMessageTimeout(TimeSpan value);
        
        // <summary>
        /// Serializer for (de)serialize messages.
        /// By default value of <see cref="ILinkBuilder.Serializer"/>
        /// </summary>
        ILinkPullConsumerBuilder Serializer(ILinkSerializer value);

        /// <summary>
        /// Assing type-name mappings for (de)serialization
        /// </summary>
        ILinkPullConsumerBuilder TypeNameMap(IDictionary<Type, string> mapping);

        /// <summary>
        /// Assigns type-name mappings for (de)serialization with builder
        /// </summary>
        ILinkPullConsumerBuilder TypeNameMap(Action<ILinkTypeNameMapBuilder> map);
    }
}