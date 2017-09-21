namespace RabbitLink.Connection
{
    /// <summary>
    /// Operational state of connection
    /// </summary>
    public enum LinkConnectionState
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
