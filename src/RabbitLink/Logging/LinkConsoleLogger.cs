#region Usings

using System;

#endregion

namespace RabbitLink.Logging
{
    /// <summary>
    ///     Implementation of <see cref="ILinkLogger" /> which produce output to <see cref="Console" />
    /// </summary>
    public class LinkConsoleLogger : BufferedLinkLogger
    {
        public LinkConsoleLogger(string name) : base(new LinkTextWriterLogger(name, Console.Out))
        {
        }
    }
}