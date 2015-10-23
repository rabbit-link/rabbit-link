#region Usings

using System;
using ColoredConsole;
using RabbitLink.Logging;

#endregion

namespace Playground
{
    internal class ColoredConsoleLinkLogger : ILinkLogger
    {
        public ColoredConsoleLinkLogger(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Name = name.Trim();
        }

        public string Name { get; }

        public void Dispose()
        {
        }

        public void Write(LinkLoggerLevel level, string message)
        {
            switch (level)
            {
                case LinkLoggerLevel.Debug:
                    ColorConsole.WriteLine("[RabbitLink:", Name.White(), ":", level.ToString(), "] ", message);
                    break;
                case LinkLoggerLevel.Info:
                    ColorConsole.WriteLine("[RabbitLink:", Name.White(), ":", level.ToString().Cyan(), "] ", message);
                    break;
                case LinkLoggerLevel.Warning:
                    ColorConsole.WriteLine("[RabbitLink:", Name.White(), ":", level.ToString().Yellow(), "] ", message);
                    break;
                case LinkLoggerLevel.Error:
                    ColorConsole.WriteLine("[RabbitLink:", Name.White(), ":", level.ToString().Red(), "] ", message);
                    break;
            }
        }
    }
}