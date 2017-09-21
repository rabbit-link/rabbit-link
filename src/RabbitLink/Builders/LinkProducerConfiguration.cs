#region Usings

using System;
using System.Threading;
using RabbitLink.Messaging;
using RabbitLink.Producer;
using RabbitLink.Serialization;
using RabbitLink.Topology;

#endregion

namespace RabbitLink.Builders
{
    internal struct LinkProducerConfiguration
    {
        private readonly LinkPublishProperties _publishProperties;
        private readonly LinkMessageProperties _messageProperties;

        public LinkProducerConfiguration(
            TimeSpan publishTimeout,
            TimeSpan recoveryInterval,
            ILinkMessageIdGenerator messageIdGenerator,
            bool confirmsMode,
            bool setUserId,
            LinkPublishProperties publishProperties,
            LinkMessageProperties messageProperties,
            ILinkProducerTopologyHandler topologyHandler,
            LinkStateHandler<LinkProducerState> stateHandler,
            ILinkSerializer serializer,
            LinkTypeNameMapping typeNameMapping
        )
        {
            if (publishTimeout < TimeSpan.Zero && publishTimeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(publishTimeout),
                    "Must be greater or equal TimeSpan.Zero or equal Timeout.InfiniteTimeSpan");

            if (recoveryInterval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(recoveryInterval), "Must be greater than TimeSpan.Zero");

            PublishTimeout = publishTimeout;
            RecoveryInterval = recoveryInterval;
            MessageIdGenerator = messageIdGenerator ?? throw new ArgumentNullException(nameof(messageIdGenerator));
            ConfirmsMode = confirmsMode;
            SetUserId = setUserId;
            _publishProperties = publishProperties ?? throw new ArgumentNullException(nameof(publishProperties));
            _messageProperties = messageProperties ?? throw new ArgumentNullException(nameof(messageProperties));
            TopologyHandler = topologyHandler ?? throw new ArgumentNullException(nameof(topologyHandler));
            StateHandler = stateHandler ?? throw new ArgumentNullException(nameof(stateHandler));
            Serializer = serializer;
            TypeNameMapping = typeNameMapping ?? throw new ArgumentNullException(nameof(typeNameMapping));
        }

        public TimeSpan PublishTimeout { get; }
        public TimeSpan RecoveryInterval { get; }
        public ILinkMessageIdGenerator MessageIdGenerator { get; }
        public bool ConfirmsMode { get; }
        public bool SetUserId { get; }
        public LinkPublishProperties PublishProperties => _publishProperties.Clone();
        public LinkMessageProperties MessageProperties => _messageProperties.Clone();
        public ILinkProducerTopologyHandler TopologyHandler { get; }
        public LinkStateHandler<LinkProducerState> StateHandler { get; }
        public ILinkSerializer Serializer { get; }
        public LinkTypeNameMapping TypeNameMapping { get; }
    }
}
