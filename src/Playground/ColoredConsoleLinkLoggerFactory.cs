#region Usings

using RabbitLink.Logging;

#endregion

namespace Playground
{
    internal class ColoredConsoleLinkLoggerFactory : ILinkLoggerFactory
    {
        public ILinkLogger CreateLogger(string name)
        {
            return new ColoredConsoleLinkLogger(name);
        }
    }
}