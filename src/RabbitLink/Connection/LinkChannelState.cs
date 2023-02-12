namespace RabbitLink.Connection
{
    /// <summary>
    /// Operational state of <see cref="ILinkChannel"/>
    /// </summary>
    public enum LinkChannelState
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
        /// Stopping
        /// </summary>
        Stopping,

        /// <summary>
        /// Disposed
        /// </summary>
        Disposed
    }
}
