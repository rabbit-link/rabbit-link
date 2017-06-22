namespace RabbitLink.Connection
{
    /// <summary>
    /// Operational state of <see cref="ILinkChannel"/>
    /// </summary>
    internal enum LinkChannelState
    {
        /// <summary>
        /// Waiting for initialization
        /// </summary>
        Init,
        /// <summary>
        /// Opening
        /// </summary>
        Opening,
        /// <summary>
        /// Reopening
        /// </summary>
        Reopening,
        /// <summary>
        /// Active processing
        /// </summary>
        Active,
        /// <summary>
        /// Stoping
        /// </summary>
        Stopping,
        /// <summary>
        /// Disposed
        /// </summary>
        Disposed
    }
}