#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Messaging;
using RabbitLink.Producer;

#endregion

namespace RabbitLink.Builders
{
    /// <summary>
    /// Configuration builder for <see cref="ILinkProducer"/>
    /// </summary>
    public interface ILinkProducerConfigurationBuilder
    {
        /// <summary>
        ///     ConfirmsMode mode (see https://www.rabbitmq.com/confirms.html)
        ///     By default use <see cref="ILinkBuilder.ProducerConfirmsMode" /> value
        /// </summary>
        ILinkProducerConfigurationBuilder ConfirmsMode(bool value);

        /// <summary>
        ///     Base message properties
        ///     By default user <see cref="ILinkBuilder.ProducerMessageProperties" /> value
        /// </summary>
        ILinkProducerConfigurationBuilder MessageProperties(LinkMessageProperties value);

        /// <summary>
        ///     Base publish properties
        /// </summary>
        ILinkProducerConfigurationBuilder PublishProperties(LinkPublishProperties value);

        /// <summary>
        ///     Publish operation timeout
        ///     By default <see cref="ILinkBuilder.ProducerPublishTimeout" /> value
        ///     null = infinite
        /// </summary>
        ILinkProducerConfigurationBuilder PublishTimeout(TimeSpan? value);

        /// <summary>
        ///     Is need to force set <see cref="LinkMessageProperties.UserId" /> from connection string to all published messages
        ///     By default <see cref="ILinkBuilder.ProducerSetUserId"/> value
        /// </summary>
        ILinkProducerConfigurationBuilder SetUserId(bool value);

        /// <summary>
        ///     MessageId generator
        ///     By default <see cref="ILinkBuilder.ProducerMessageIdGenerator" /> used
        /// </summary>
        ILinkProducerConfigurationBuilder MessageIdGenerator(ILinkMessageIdGenerator value);
    }
}