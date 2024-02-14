#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Consumer;
using RabbitLink.Interceptors;
using RabbitLink.Serialization;
using RabbitLink.Topology;

#endregion

namespace RabbitLink.Builders
{
    internal readonly struct LinkConsumerConfiguration
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
            LinkConsumerMessageHandlerDelegate<ReadOnlyMemory<byte>> messageHandler,
            ILinkSerializer serializer,
            ConsumerTagProviderDelegate consumerTagProvider,
            IReadOnlyList<IDeliveryInterceptor> deliveryInterceptors
        )
        {
            if (recoveryInterval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(recoveryInterval), "Must be greater than TimeSpan.Zero");

            if (prefetchCount == 0)
                throw new ArgumentOutOfRangeException(nameof(prefetchCount), "Must be greater than 0");

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
            ConsumerTagProvider = consumerTagProvider;
            DeliveryInterceptors = deliveryInterceptors ?? Array.Empty<IDeliveryInterceptor>();
        }

        public TimeSpan RecoveryInterval { get; }
        public ushort PrefetchCount { get; }
        public bool AutoAck { get; }
        public bool CancelOnHaFailover { get; }
        public bool Exclusive { get; }
        public LinkConsumerMessageHandlerDelegate<ReadOnlyMemory<byte>> MessageHandler { get; }
        public ILinkConsumerErrorStrategy ErrorStrategy { get; }
        public int Priority { get; }
        public ILinkConsumerTopologyHandler TopologyHandler { get; }
        public LinkStateHandler<LinkConsumerState> StateHandler { get; }
        public ILinkSerializer Serializer { get; }
        public ConsumerTagProviderDelegate ConsumerTagProvider { get; }
        public IReadOnlyList<IDeliveryInterceptor> DeliveryInterceptors { get; }
    }
}
