using System;
using System.Collections.Generic;
using System.Text;
using RabbitLink.Configuration;
using RabbitLink.Connection;
using RabbitLink.Internals;
using RabbitLink.Topology;

namespace RabbitLink.Consumer
{
    internal class LinkPushConsumer:AsyncStateMachine<LinkConsumerState>, ILinkPushConsumerInternal
    {
        public LinkPushConsumer(
            LinkConsumerConfiguration configuration,
            LinkConfiguration linkConfiguration,
            ILinkChannel channel,
            ILinkConsumerTopologyHandler topologyHandler
            
        ) : base(LinkConsumerState.Init)
        {
            
        }
         
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Guid Id { get; }
        public ushort PrefetchCount { get; }
        public bool AutoAck { get; }
        public int Priority { get; }
        public bool CancelOnHaFailover { get; }
        public bool Exclusive { get; }
        public event EventHandler Disposed;
    }
}
