#region Usings

using RabbitLink.Consumer;
using System;
using System.Collections.Generic;
using RabbitLink.Connection;
using RabbitLink.Interceptors;
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
        /// Gets new pull consumer builder.
        /// All properties except handler will be inherited.
        /// </summary>
        ILinkPullConsumerBuilder Pull { get; }

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
        ///     By default 1
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
        /// Message handler for certain type
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
        ILinkConsumerBuilder Handler(LinkConsumerMessageHandlerDelegate<object> value);

        /// <summary>
        /// Sets interception delegate on message delivery.
        /// </summary>
        ILinkConsumerBuilder WithInterception(IDeliveryInterceptor value);

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

        /// <summary>
        /// Assign type-name mappings for (de)serialization
        /// </summary>
        ILinkConsumerBuilder TypeNameMap(IDictionary<Type, string> mapping);

        /// <summary>
        /// Assigns type-name mappings for (de)serialization with builder
        /// </summary>
        ILinkConsumerBuilder TypeNameMap(Action<ILinkTypeNameMapBuilder> map);

        /// <summary>
        /// Assigns delegate that provides consumer tag.
        /// </summary>
        ILinkConsumerBuilder ConsumerTag(ConsumerTagProviderDelegate tagProviderDelegate);
    }
}
