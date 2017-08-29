#region Usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitLink.Connection;
using RabbitLink.Messaging;
using RabbitLink.Producer;
using RabbitLink.Topology;

#endregion

namespace RabbitLink.Builders
{
    /// <summary>
    /// Builder for <see cref="ILinkProducer"/>
    /// </summary>
    public interface ILinkProducerBuilder
    {
        /// <summary>
        /// Builds <see cref="ILinkProducer"/> instance
        /// </summary>
        ILinkProducer Build();

        /// <summary>
        ///     ConfirmsMode mode (see https://www.rabbitmq.com/confirms.html)
        ///     By default false
        /// </summary>
        ILinkProducerBuilder ConfirmsMode(bool value);

        /// <summary>
        ///     Base message properties
        /// </summary>
        ILinkProducerBuilder MessageProperties(LinkMessageProperties value);

        /// <summary>
        ///     Base publish properties
        /// </summary>
        ILinkProducerBuilder PublishProperties(LinkPublishProperties value);

        /// <summary>
        ///     Publish operation timeout
        ///     By default <see cref="TimeSpan.Zero"/> = Infinite
        /// </summary>
        ILinkProducerBuilder PublishTimeout(TimeSpan value);

        /// <summary>
        /// Channel / Topology recovery interval
        /// By default <see cref="ILinkBuilder.RecoveryInterval"/>
        /// </summary>
        ILinkProducerBuilder RecoveryInterval(TimeSpan value);

        /// <summary>
        ///     Is need to force set <see cref="LinkMessageProperties.UserId" /> from connection string to all published messages
        ///     By default true
        /// </summary>
        ILinkProducerBuilder SetUserId(bool value);

        /// <summary>
        ///     MessageId generator
        ///     By default <see cref="LinkGuidMessageIdGenerator"/>
        /// </summary>
        ILinkProducerBuilder MessageIdGenerator(ILinkMessageIdGenerator value);

        /// <summary>
        /// Sets topology handler for queue
        /// </summary>
        ILinkProducerBuilder Exchange(LinkProducerTopologyConfigDelegate config);

        /// <summary>
        /// Sets topology handler for queue and topology exception handler
        /// </summary>
        ILinkProducerBuilder Exchange(LinkProducerTopologyConfigDelegate config, LinkTopologyErrorDelegate error);

        /// <summary>
        ///  Sets topology handler
        /// </summary>
        ILinkProducerBuilder Exchange(ILinkProducerTopologyHandler handler);
        
        /// <summary>
        /// Sets handler for state changes
        /// </summary>
        ILinkProducerBuilder OnStateChange(LinkStateHandler<LinkProducerState> value);
        
        /// <summary>
        /// Sets handler for channel state changes
        /// </summary>
        ILinkProducerBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> value);

    }
}