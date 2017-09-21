namespace RabbitLink.Topology
{
    /// <summary>
    /// State of <see cref="ILinkTopology"/>
    /// </summary>
    public enum LinkTopologyState
    {
        /// <summary>
        /// Initializing
        /// </summary>
        Init,

        /// <summary>
        /// Configuring channel and topology
        /// </summary>
        Configuring,

        /// <summary>
        /// Reconfiguring channel and topology
        /// </summary>
        Reconfiguring,

        /// <summary>
        /// Topology sucessfully configured
        /// </summary>
        Ready,

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
