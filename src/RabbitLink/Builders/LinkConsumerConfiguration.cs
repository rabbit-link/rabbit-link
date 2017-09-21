#region Usings

using System;
using RabbitLink.Consumer;
using RabbitLink.Serialization;
using RabbitLink.Topology;

#endregion

namespace RabbitLink.Builders
{
    internal struct LinkConsumerConfiguration
    {
        public LinkConsumerConfiguration(
            TimeSpan recoveryInterval,
            ushort prefetchCount,
            bool autoAck,
            int priority,
            bool cancelOnHaFailover,
            bool exclusive,
            ILinkConsumerTopologyHandler topologyHandler,
            LinkStateHandler<LinkConsumerState> stateHandler,
            ILinkConsumerErrorStrategy errorStrategy,
            LinkConsumerMessageHandlerDelegate<byte[]> messageHandler,
            ILinkSerializer serializer
        )
        {
            if (recoveryInterval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(recoveryInterval), "Must be greater than TimeSpan.Zero");

            RecoveryInterval = recoveryInterval;
            PrefetchCount = prefetchCount;
            AutoAck = autoAck;
            Priority = priority;
            CancelOnHaFailover = cancelOnHaFailover;
            Exclusive = exclusive;
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            ErrorStrategy = errorStrategy ?? throw new ArgumentNullException(nameof(errorStrategy));
            TopologyHandler = topologyHandler ?? throw new ArgumentNullException(nameof(topologyHandler));
            StateHandler = stateHandler ?? throw new ArgumentNullException(nameof(stateHandler));
            Serializer = serializer;
        }

        public TimeSpan RecoveryInterval { get; }
        public ushort PrefetchCount { get; }
        public bool AutoAck { get; }
        public bool CancelOnHaFailover { get; }
        public bool Exclusive { get; }
        public LinkConsumerMessageHandlerDelegate<byte[]> MessageHandler { get; }
        public ILinkConsumerErrorStrategy ErrorStrategy { get; }
        public int Priority { get; }
        public ILinkConsumerTopologyHandler TopologyHandler { get; }
        public LinkStateHandler<LinkConsumerState> StateHandler { get; }
        public ILinkSerializer Serializer { get; }
    }
}
