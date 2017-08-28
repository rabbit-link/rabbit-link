namespace RabbitLink.Consumer
{
    /// <summary>
    /// State of <see cref="ILinkConsumer"/>
    /// </summary>
    public enum LinkConsumerState
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