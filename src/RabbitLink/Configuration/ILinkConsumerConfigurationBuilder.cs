#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    public interface ILinkConsumerConfigurationBuilder
    {
        /// <summary>
        ///     Message prefetch count
        ///     By default use <see cref="ILinkConfigurationBuilder.ConsumerPrefetchCount" /> value
        /// </summary>
        ILinkConsumerConfigurationBuilder PrefetchCount(ushort value);

        /// <summary>
        ///     Auto ack on consume
        ///     By default use <see cref="ILinkConfigurationBuilder.ConsumerAutoAck" /> value
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
        ///     By default <see cref="ILinkConfigurationBuilder.ConsumerCancelOnHaFailover" /> value
        /// </summary>
        ILinkConsumerConfigurationBuilder CancelOnHaFailover(bool value);

        /// <summary>
        ///     Is consumer must be exclusive
        ///     By default false
        /// </summary>
        ILinkConsumerConfigurationBuilder Exclusive(bool value);

        /// <summary>
        ///     Default <see cref="ILinkMessageSerializer" /> for serialization / deserialization
        ///     By default <see cref="ILinkConfigurationBuilder.MessageSerializer" /> value
        /// </summary>
        ILinkConsumerConfigurationBuilder MessageSerializer(ILinkMessageSerializer value);


        ILinkConsumerConfigurationBuilder TypeNameMap(IDictionary<Type, string> values);
        ILinkConsumerConfigurationBuilder TypeNameMap(Action<ILinkConfigurationTypeNameMapBuilder> map);
    }
}