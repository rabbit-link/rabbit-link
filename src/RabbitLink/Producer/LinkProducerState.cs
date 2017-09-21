namespace RabbitLink.Producer
{
    /// <summary>
    /// State of <see cref="ILinkProducer"/>
    /// </summary>
    public enum LinkProducerState
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
        /// Active
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
