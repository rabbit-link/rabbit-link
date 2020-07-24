#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Connection;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkTopologyBuilder : ILinkTopologyBuilder
    {
        private readonly LinkStateHandler<LinkChannelState> _channelStateHandler;
        private readonly Link _link;

        private readonly TimeSpan _recoveryInterval;
        private readonly LinkStateHandler<LinkTopologyState> _stateHandler;
        private readonly ILinkTopologyHandler _topologyHandler;

        public LinkTopologyBuilder(
            Link link,
            TimeSpan recoveryInterval,
            LinkStateHandler<LinkTopologyState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null,
            ILinkTopologyHandler topologyHandler = null
        )
        {
            _link = link ?? throw new ArgumentNullException(nameof(link));

            _recoveryInterval = recoveryInterval;
            _stateHandler = stateHandler ?? ((old, @new) => { });
            _channelStateHandler = channelStateHandler ?? ((old, @new) => { });
            _topologyHandler = topologyHandler;
        }

        private LinkTopologyBuilder(
            LinkTopologyBuilder prev,
            TimeSpan? recoveryInterval = null,
            LinkStateHandler<LinkTopologyState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null,
            ILinkTopologyHandler topologyHandler = null
        ) : this(
            prev._link,
            recoveryInterval ?? prev._recoveryInterval,
            stateHandler ?? prev._stateHandler,
            channelStateHandler ?? prev._channelStateHandler,
            topologyHandler ?? prev._topologyHandler
        )
        {
        }


        public ILinkTopologyBuilder RecoveryInterval(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than TimeSpan.Zero");

            return new LinkTopologyBuilder(this, recoveryInterval: value);
        }

        public ILinkTopologyBuilder OnStateChange(LinkStateHandler<LinkTopologyState> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkTopologyBuilder(this, stateHandler: value);
        }

        public ILinkTopologyBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkTopologyBuilder(this, channelStateHandler: value);
        }

        public ILinkTopologyBuilder Handler(ILinkTopologyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkTopologyBuilder(this, topologyHandler: handler);
        }

        public ILinkTopologyBuilder Handler(LinkTopologyConfigDelegate config)
        {
            return Handler(config, () => Task.CompletedTask);
        }

        public ILinkTopologyBuilder Handler(LinkTopologyConfigDelegate config, LinkTopologyReadyDelegate ready)
        {
            return Handler(config, ready, ex => Task.CompletedTask);
        }

        public ILinkTopologyBuilder Handler(
            LinkTopologyConfigDelegate config,
            LinkTopologyReadyDelegate ready,
            LinkTopologyErrorDelegate error
        )
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (ready == null)
                throw new ArgumentNullException(nameof(ready));

            if (error == null)
                throw new ArgumentNullException(nameof(error));

            return Handler(new LinkTopologyHandler(config, ready, error));
        }

        public ILinkTopology Build()
        {
            if (_topologyHandler == null)
                throw new InvalidOperationException("Queue must be set");

            var config = new LinkTopologyConfiguration(
                _recoveryInterval,
                _stateHandler,
                _topologyHandler
            );

            return new LinkTopology(_link.CreateChannel(_channelStateHandler, config.RecoveryInterval), config);
        }

        public async Task WaitAsync(CancellationToken? cancellation = null)
        {
            using (var topology = Build())
            {
                await topology.WaitReadyAsync(cancellation)
                    .ConfigureAwait(false);
            }
        }
    }
}
