#region Usings

using RabbitLink.Consumer;
using System;
using System.Collections.Generic;
using RabbitLink.Connection;
using RabbitLink.Serialization;
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
        /// Message handler for concrete type
        /// </summary>
        ILinkConsumerBuilder Handler<TBody>(LinkConsumerMessageHandlerDelegate<TBody> value)
            where TBody : class;

        /// <summary>
        /// Raw message handler
        /// </summary>
        ILinkConsumerBuilder Handler(LinkConsumerMessageHandlerDelegate<byte[]> value);
        
        /// <summary>
        /// Type name mapping message handler
        /// </summary>
        /// <param name="value">Handler delegate</param>
        /// <param name="mapping">Type name mapping</param>
        ILinkConsumerBuilder Handler(LinkConsumerMessageHandlerDelegate<object> value, IDictionary<Type, string> mapping);
        
        /// <summary>
        /// Type name mapping message handler
        /// </summary>
        /// <param name="value">Handler delegate</param>
        /// <param name="map">Type name mapping builder</param>
        ILinkConsumerBuilder Handler(LinkConsumerMessageHandlerDelegate<object> value, Action<ILinkTypeNameMapBuilder> map);

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

        /// <summary>
        /// Serializer for (de)serialize messages.
        /// By default value of <see cref="ILinkBuilder.Serializer"/>
        /// </summary>
        ILinkConsumerBuilder Serializer(ILinkSerializer value);
    }
}