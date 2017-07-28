#region Usings

using RabbitLink.Logging;

#endregion

namespace Playground
{
    internal class ConsoleLinkLoggerFactory : ILinkLoggerFactory
    {
        public ILinkLogger CreateLogger(string name)
        {
            return new ConsoleLinkLogger(name);
        }
    }
}