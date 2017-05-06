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
        Open,
        /// <summary>
        /// Reopening
        /// </summary>
        Reopen,
        /// <summary>
        /// Active processing
        /// </summary>
        Active,
        /// <summary>
        /// Stoping
        /// </summary>
        Stop,
        /// <summary>
        /// Disposed
        /// </summary>
        Disposed
    }
}