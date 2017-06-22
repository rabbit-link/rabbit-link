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
        Opening,
        /// <summary>
        /// Reopening
        /// </summary>
        Reopening,
        /// <summary>
        /// Active
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