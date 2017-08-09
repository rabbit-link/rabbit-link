#region Usings

using System;
using RabbitLink.Connection;
using RabbitLink.Messaging;
using RabbitLink.Producer;
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
            LinkStateHandler<LinkProducerState> stateHandler
        )
        {
            PublishTimeout = publishTimeout;
            RecoveryInterval = recoveryInterval;
            MessageIdGenerator = messageIdGenerator;
            ConfirmsMode = confirmsMode;
            SetUserId = setUserId;
            _publishProperties = publishProperties;
            _messageProperties = messageProperties;
            TopologyHandler = topologyHandler;
            StateHandler = stateHandler;
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
    }
}