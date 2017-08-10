﻿#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Connection;
using RabbitLink.Messaging;
using RabbitLink.Producer;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkProducerBuilder : ILinkProducerBuilder
    {
        private readonly Link _link;

        private readonly bool _confirmsMode;
        private readonly bool _setUserId;
        private readonly TimeSpan _publishTimeout;
        private readonly TimeSpan _recoveryInterval;
        private readonly ILinkMessageIdGenerator _messageIdGenerator;
        private readonly LinkMessageProperties _messageProperties;
        private readonly LinkPublishProperties _publishProperties;
        private readonly ILinkProducerTopologyHandler _topologyHandler;
        private readonly LinkStateHandler<LinkProducerState> _stateHandler;
        private readonly LinkStateHandler<LinkChannelState> _channelStateHandler;

        public LinkProducerBuilder
        (
            Link link,
            TimeSpan recoveryInterval,
            TimeSpan? publishTimeout = null,
            bool? confirmsMode = null,
            bool? setUserId = null,
            ILinkMessageIdGenerator messageIdGenerator = null,
            LinkPublishProperties publishProperties = null,
            LinkMessageProperties messageProperties = null,
            ILinkProducerTopologyHandler topologyHandler = null,
            LinkStateHandler<LinkProducerState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null
        )
        {
            _link = link ?? throw new ArgumentNullException(nameof(link));

            _confirmsMode = confirmsMode ?? false;
            _setUserId = setUserId ?? true;
            _publishTimeout = publishTimeout ?? TimeSpan.Zero;
            _recoveryInterval = recoveryInterval;
            _messageIdGenerator = messageIdGenerator ?? new LinkGuidMessageIdGenerator();
            _publishProperties = publishProperties ?? new LinkPublishProperties();
            _messageProperties = messageProperties ?? new LinkMessageProperties();
            _topologyHandler = topologyHandler;
            _stateHandler = stateHandler ?? ((old, @new) => { });
            _channelStateHandler = channelStateHandler ?? ((old, @new) => { });
        }

        private LinkProducerBuilder
        (
            LinkProducerBuilder prev,
            TimeSpan? recoveryInterval = null,
            TimeSpan? publishTimeout = null,
            bool? confirmsMode = null,
            bool? setUserId = null,
            ILinkMessageIdGenerator messageIdGenerator = null,
            LinkPublishProperties publishProperties = null,
            LinkMessageProperties messageProperties = null,
            ILinkProducerTopologyHandler topologyHandler = null,
            LinkStateHandler<LinkProducerState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null
        ) : this
            (
                prev._link,
                recoveryInterval ?? prev._recoveryInterval,
                publishTimeout ?? prev._publishTimeout,
                confirmsMode ?? prev._confirmsMode,
                setUserId ?? prev._setUserId,
                messageIdGenerator ?? prev._messageIdGenerator,
                publishProperties ?? prev._publishProperties.Clone(),
                messageProperties ?? prev._messageProperties.Clone(),
                topologyHandler ?? prev._topologyHandler,
                stateHandler ?? prev._stateHandler,
                channelStateHandler ?? prev._channelStateHandler
            )
        {
        }

        public ILinkProducerBuilder ConfirmsMode(bool value)
        {
            return new LinkProducerBuilder(this, confirmsMode: value);
        }

        public ILinkProducerBuilder MessageProperties(LinkMessageProperties value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkProducerBuilder(this, messageProperties: value.Clone());
        }

        public ILinkProducerBuilder PublishProperties(LinkPublishProperties value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkProducerBuilder(this, publishProperties: value.Clone());
        }

        public ILinkProducerBuilder PublishTimeout(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater or equal TimeSpan.Zero");

            return new LinkProducerBuilder(this, publishTimeout: value);
        }

        public ILinkProducerBuilder RecoveryInterval(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than TimeSpan.Zero");

            return new LinkProducerBuilder(this, recoveryInterval: value);
        }

        public ILinkProducerBuilder SetUserId(bool value)
        {
            return new LinkProducerBuilder(this, setUserId: value);
        }

        public ILinkProducerBuilder MessageIdGenerator(ILinkMessageIdGenerator value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkProducerBuilder(this, messageIdGenerator: value);
        }

        public ILinkProducerBuilder Queue(LinkProducerTopologyConfigDelegate config)
        {
            return Queue(config, ex => Task.CompletedTask);
        }

        public ILinkProducerBuilder Queue(LinkProducerTopologyConfigDelegate config, LinkTopologyErrorDelegate error)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (error == null)
                throw new ArgumentNullException(nameof(error));

            return Queue(new LinkProducerTopologyHandler(config, error));
        }

        public ILinkProducerBuilder Queue(ILinkProducerTopologyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkProducerBuilder(this, topologyHandler: handler);
        }

        public ILinkProducerBuilder OnStateChange(LinkStateHandler<LinkProducerState> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkProducerBuilder(this, stateHandler: handler);
        }

        public ILinkProducerBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> handler)
        {
            if(handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            return new LinkProducerBuilder(this, channelStateHandler: handler);
        }


        public ILinkProducer Build()
        {
            if (_topologyHandler == null)
                throw new InvalidOperationException("Queue must be set");

            var config = new LinkProducerConfiguration(
                _publishTimeout,
                _recoveryInterval,
                _messageIdGenerator,
                _confirmsMode,
                _setUserId,
                _publishProperties.Clone(),
                _messageProperties.Clone(),
                _topologyHandler,
                _stateHandler
            );

            return new LinkProducer(config, _link.CreateChannel(_channelStateHandler, config.RecoveryInterval));
        }
    }
}