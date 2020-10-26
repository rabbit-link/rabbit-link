namespace RabbitLink.Logging
{
    /// <summary>
    ///     Implementation of <see cref="ILinkLoggerFactory" /> which using <see cref="LinkNullLogger" /> as logger
    /// </summary>
    public sealed class LinkNullLoggingFactory : ILinkLoggerFactory
    {
        /// <summary>
        ///     Returns new instance of <see cref="ILinkLogger" />
        /// </summary>
        /// <param name="name">name of logger</param>
        public ILinkLogger CreateLogger(string name)
        {
            return new LinkNullLogger();
        }
    }
}
