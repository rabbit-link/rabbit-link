namespace RabbitLink.Topology
{
    /// <summary>
    /// Type of RabbitMQ exchage
    /// </summary>
    public enum LinkExchangeType
    {
        /// <summary>
        /// Direct
        /// </summary>
        Direct,
        
        /// <summary>
        /// Fanout
        /// </summary>
        Fanout,
        
        /// <summary>
        /// Topic
        /// </summary>
        Topic,
        
        /// <summary>
        /// Headers
        /// </summary>
        Headers
    }
}