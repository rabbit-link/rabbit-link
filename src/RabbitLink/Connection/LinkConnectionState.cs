namespace RabbitLink.Connection
{
    /// <summary>
    /// Operational state of <see cref="ILinkConnection"/>
    /// </summary>
    internal enum LinkConnectionState
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
        /// Active
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