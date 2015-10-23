#region Usings

using System;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkActionsTopologyHandler : ILinkTopologyHandler
    {
        private readonly Func<Exception, Task> _configurationError;
        private readonly Func<ILinkTopologyConfig, Task> _configure;
        private readonly Func<Task> _ready;

        public LinkActionsTopologyHandler(Func<ILinkTopologyConfig, Task> configure, Func<Task> ready,
            Func<Exception, Task> configurationError)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            if (configurationError == null)
                throw new ArgumentNullException(nameof(configurationError));

            if (ready == null)
                throw new ArgumentNullException(nameof(ready));

            _configure = configure;
            _configurationError = configurationError;
            _ready = ready;
        }

        public Task Configure(ILinkTopologyConfig config)
        {
            return _configure(config);
        }

        public Task Ready()
        {
            return _ready();
        }

        public Task ConfigurationError(Exception ex)
        {
            return _configurationError(ex);
        }
    }
}