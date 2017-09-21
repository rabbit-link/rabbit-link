namespace RabbitLink.Logging
{
    /// <summary>
    ///     Implementation of <see cref="ILinkLogger" /> which does nothing
    /// </summary>
    public sealed class LinkNullLogger : ILinkLogger
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        public void Write(LinkLoggerLevel level, string message)
        {
            // No operation            
        }

        /// <summary>
        /// Does nonthing
        /// </summary>
        public void Dispose()
        {
            // No operation            
        }
    }
}
