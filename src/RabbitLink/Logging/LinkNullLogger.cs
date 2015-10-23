namespace RabbitLink.Logging
{
    /// <summary>
    ///     Implementation of <see cref="ILinkLogger" /> which produces no output
    /// </summary>
    public sealed class LinkNullLogger : ILinkLogger
    {
        public void Write(LinkLoggerLevel level, string message)
        {
            // No operation            
        }

        public void Dispose()
        {
            // No operation            
        }
    }
}