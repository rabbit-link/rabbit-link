#region Usings

using System;
using RabbitLink.Logging;

#endregion

namespace Playground
{
    internal class ConsoleLinkLogger : ILinkLogger
    {
        public ConsoleLinkLogger(string name)
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
            Console.WriteLine("[RabbitLink:{0}:{1}] {2}", Name, level, message);
        }
    }
}