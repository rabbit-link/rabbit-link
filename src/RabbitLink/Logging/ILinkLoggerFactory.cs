namespace RabbitLink.Logging
{
    /// <summary>
    ///     Factory for <see cref="ILinkLogger" />
    /// </summary>
    public interface ILinkLoggerFactory
    {
        /// <summary>
        ///     Gets new instance of <see cref="ILinkLogger" />
        /// </summary>
        /// <param name="name">Name of <see cref="ILinkLogger" /></param>
        /// <returns>new <see cref="ILinkLogger" /> instance</returns>
        ILinkLogger CreateLogger(string name);
    }
}