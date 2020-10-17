using Microsoft.Extensions.Logging;

namespace RabbitLink.Logging
{
    internal class LoggerFactory : ILinkLoggerFactory
    {
        private readonly ILoggerFactory _factory;
        private readonly string _categoryPrefix;

        public LoggerFactory(ILoggerFactory factory, string categoryPrefix)
        {
            _factory = factory;
            _categoryPrefix = categoryPrefix;
        }

        public ILinkLogger CreateLogger(string name) => new Logger(_factory.CreateLogger($"{_categoryPrefix}{name}"));

    }
}
