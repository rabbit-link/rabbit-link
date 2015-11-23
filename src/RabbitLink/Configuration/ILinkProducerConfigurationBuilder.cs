#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    public interface ILinkProducerConfigurationBuilder
    {
        /// <summary>
        ///     ConfirmsMode mode (see https://www.rabbitmq.com/confirms.html)
        ///     By default use <see cref="ILinkConfigurationBuilder.ProducerConfirmsMode" /> value
        /// </summary>
        ILinkProducerConfigurationBuilder ConfirmsMode(bool value);

        /// <summary>
        ///     Base message properties
        ///     By default user <see cref="ILinkConfigurationBuilder.ProducerMessageProperties" /> value
        /// </summary>
        ILinkProducerConfigurationBuilder MessageProperties(LinkMessageProperties value);

        /// <summary>
        ///     Base publish properties
        /// </summary>
        ILinkProducerConfigurationBuilder PublishProperties(LinkPublishProperties value);

        /// <summary>
        ///     Publish operation timeout
        ///     By default <see cref="ILinkConfigurationBuilder.ProducerPublishTimeout" /> value
        ///     null = infinite
        /// </summary>
        ILinkProducerConfigurationBuilder PublishTimeout(TimeSpan? value);

        /// <summary>
        ///     Default <see cref="ILinkMessageSerializer" /> for serialization / deserialization
        ///     By default <see cref="ILinkConfigurationBuilder.MessageSerializer" /> value
        /// </summary>
        ILinkProducerConfigurationBuilder MessageSerializer(ILinkMessageSerializer value);

        /// <summary>
        ///     Is need to force set <see cref="LinkMessageProperties.UserId" /> from connection string to all published messages
        ///     By default <see cref="ILinkConfigurationBuilder.ProducerSetUserId"/> value
        /// </summary>
        ILinkProducerConfigurationBuilder SetUserId(bool value);


        ILinkProducerConfigurationBuilder TypeNameMap(IDictionary<Type, string> values);
        ILinkProducerConfigurationBuilder TypeNameMap(Action<ILinkConfigurationTypeNameMapBuilder> map);
    }
}