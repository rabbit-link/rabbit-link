#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Connection;
using RabbitLink.Interceptors;
using RabbitLink.Messaging;
using RabbitLink.Producer;
using RabbitLink.Serialization;
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
        private readonly ILinkSerializer _serializer;
        private readonly LinkTypeNameMapping _typeNameMapping;
        private readonly IReadOnlyList<IPublishInterceptor> _publishInterceptors;

        public LinkProducerBuilder
        (
            Link link,
            TimeSpan recoveryInterval,
            ILinkSerializer serializer,
            TimeSpan? publishTimeout = null,
            bool? confirmsMode = null,
            bool? setUserId = null,
            ILinkMessageIdGenerator messageIdGenerator = null,
            LinkPublishProperties publishProperties = null,
            LinkMessageProperties messageProperties = null,
            ILinkProducerTopologyHandler topologyHandler = null,
            LinkStateHandler<LinkProducerState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null,
            LinkTypeNameMapping typeNameMapping = null,
            IReadOnlyList<IPublishInterceptor> publishInterceptors = null
        )
        {
            _link = link ?? throw new ArgumentNullException(nameof(link));

            _confirmsMode = confirmsMode ?? false;
            _setUserId = setUserId ?? true;
            _publishTimeout = publishTimeout ?? Timeout.InfiniteTimeSpan;
            _recoveryInterval = recoveryInterval;
            _messageIdGenerator = messageIdGenerator ?? new LinkGuidMessageIdGenerator();
            _publishProperties = publishProperties ?? new LinkPublishProperties();
            _messageProperties = messageProperties ?? new LinkMessageProperties();
            _topologyHandler = topologyHandler;
            _publishInterceptors = publishInterceptors;
            _stateHandler = stateHandler ?? ((_, _) => { });
            _channelStateHandler = channelStateHandler ?? ((_, _) => { });
            _serializer = serializer;
            _typeNameMapping = typeNameMapping ?? new LinkTypeNameMapping();
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
            LinkStateHandler<LinkChannelState> channelStateHandler = null,
            ILinkSerializer serializer = null,
            LinkTypeNameMapping typeNameMapping = null,
            IPublishInterceptor[] publishInterceptors = null
        ) : this
            (
                prev._link,
                recoveryInterval ?? prev._recoveryInterval,
                serializer ?? prev._serializer,
                publishTimeout ?? prev._publishTimeout,
                confirmsMode ?? prev._confirmsMode,
                setUserId ?? prev._setUserId,
                messageIdGenerator ?? prev._messageIdGenerator,
                publishProperties ?? prev._publishProperties.Clone(),
                messageProperties ?? prev._messageProperties.Clone(),
                topologyHandler ?? prev._topologyHandler,
                stateHandler ?? prev._stateHandler,
                channelStateHandler ?? prev._channelStateHandler,
                typeNameMapping ?? prev._typeNameMapping,
                publishInterceptors ?? prev._publishInterceptors
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
            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Must be greater or equal TimeSpan.Zero or equal Timeout.InfiniteTimeSpan");

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

        public ILinkProducerBuilder Exchange(LinkProducerTopologyConfigDelegate config)
        {
            return Exchange(config, _ => Task.CompletedTask);
        }

        public ILinkProducerBuilder Exchange(LinkProducerTopologyConfigDelegate config, LinkTopologyErrorDelegate error)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (error == null)
                throw new ArgumentNullException(nameof(error));

            return Exchange(new LinkProducerTopologyHandler(config, error));
        }

        public ILinkProducerBuilder Exchange(ILinkProducerTopologyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkProducerBuilder(this, topologyHandler: handler);
        }

        public ILinkProducerBuilder OnStateChange(LinkStateHandler<LinkProducerState> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkProducerBuilder(this, stateHandler: value);
        }

        public ILinkProducerBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkProducerBuilder(this, channelStateHandler: value);
        }

        public ILinkProducerBuilder Serializer(ILinkSerializer value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkProducerBuilder(this, serializer: value);
        }

        public ILinkProducerBuilder TypeNameMap(IDictionary<Type, string> mapping)
            => TypeNameMap(map => map.Set(mapping));

        public ILinkProducerBuilder TypeNameMap(Action<ILinkTypeNameMapBuilder> map)
        {
            var builder = new LinkTypeNameMapBuilder(_typeNameMapping);
            map?.Invoke(builder);

            return new LinkProducerBuilder(this, typeNameMapping: builder.Build());
        }

        /// <inheritdoc />
        public ILinkProducerBuilder WithInterception(IPublishInterceptor value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (_publishInterceptors == null)
                return new LinkProducerBuilder(this, publishInterceptors: new[] { value });

            var newInterceptors = _publishInterceptors.Concat(new[] { value })
                                                    .ToArray();
            return new LinkProducerBuilder(this, publishInterceptors: newInterceptors);
        }

        public ILinkProducer Build()
        {
            if (_topologyHandler == null)
                throw new InvalidOperationException("Exchange must be set");

            var config = new LinkProducerConfiguration(
                _publishTimeout,
                _recoveryInterval,
                _messageIdGenerator,
                _confirmsMode,
                _setUserId,
                _publishProperties.Clone(),
                _messageProperties.Clone(),
                _topologyHandler,
                _stateHandler,
                _serializer,
                _typeNameMapping,
                _publishInterceptors
            );

            return new LinkProducer(config, _link.CreateChannel(_channelStateHandler, config.RecoveryInterval));
        }
    }
}
