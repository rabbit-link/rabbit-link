#region Usings

using System;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkTopologyHandler : ILinkTopologyHandler
    {
        private readonly LinkTopologyErrorDelegate _errorAction;
        private readonly LinkTopologyConfigDelegate _configAction;
        private readonly LinkTopologyReadyDelegate _readyAction;

        public LinkTopologyHandler(LinkTopologyConfigDelegate configAction, LinkTopologyReadyDelegate readyAction,
            LinkTopologyErrorDelegate errorAction)
        {
            _configAction = configAction ?? throw new ArgumentNullException(nameof(configAction));
            _errorAction = errorAction ?? throw new ArgumentNullException(nameof(errorAction));
            _readyAction = readyAction ?? throw new ArgumentNullException(nameof(readyAction));
        }

        public Task Configure(ILinkTopologyConfig config)
        {
            return _configAction(config);
        }

        public Task Ready()
        {
            return _readyAction();
        }

        public Task ConfigurationError(Exception ex)
        {
            return _errorAction(ex);
        }
    }
}