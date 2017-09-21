using System;
using RabbitLink.Topology;

namespace RabbitLink.Builders
{
    internal struct LinkTopologyConfiguration
    {
        public LinkTopologyConfiguration(
            TimeSpan recoveryInterval,
            LinkStateHandler<LinkTopologyState> stateHandler,
            ILinkTopologyHandler topologyHandler
        )
        {
            if (recoveryInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(recoveryInterval), "Must be greater than TimeSpan.Zero");

            RecoveryInterval = recoveryInterval;
            StateHandler = stateHandler ?? throw new ArgumentNullException(nameof(stateHandler));
            TopologyHandler = topologyHandler ?? throw new ArgumentException(nameof(topologyHandler));
        }

        public TimeSpan RecoveryInterval { get; }
        public LinkStateHandler<LinkTopologyState> StateHandler { get; }
        public ILinkTopologyHandler TopologyHandler { get; }
    }
}
