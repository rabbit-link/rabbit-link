using System;
using System.Threading.Tasks;

namespace RabbitLink.Topology.Internal
{
    internal class LinkConsumerTopologyHandler : ILinkConsumerTopologyHandler
    {
        private readonly LinkConsumerTopologyConfigDelegate _configAction;
        private readonly LinkTopologyErrorDelegate _errorAction;

        public LinkConsumerTopologyHandler(LinkConsumerTopologyConfigDelegate configAction,
            LinkTopologyErrorDelegate errorAction)
        {
            _configAction = configAction ?? throw new ArgumentNullException(nameof(configAction));
            _errorAction = errorAction ?? throw new ArgumentNullException(nameof(errorAction));
        }

        public Task<ILinkQueue> Configure(ILinkTopologyConfig config)
        {
            return _configAction(config);
        }

        public Task ConfigurationError(Exception ex)
        {
            return _errorAction(ex);
        }
    }
}
