#region Usings

using System;
using System.IO;

#endregion

namespace RabbitLink.Logging
{
    /// <summary>
    ///     Implementation of <see cref="ILinkLogger" /> which produce output to <see cref="TextWriter" />
    /// </summary>
    public class LinkTextWriterLogger : ILinkLogger
    {
        private readonly TextWriter _writer;

        public LinkTextWriterLogger(string name, TextWriter writer)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            Name = name;
            _writer = writer;
        }

        /// <summary>
        ///     Name of logger
        /// </summary>
        public string Name { get; }

        public void Write(LinkLoggerLevel level, string message)
        {
            _writer.WriteLine($"[RabbitLink:{Name}:{level}] {message}");
            _writer.Flush();
        }

        public void Dispose()
        {
        }
    }
}