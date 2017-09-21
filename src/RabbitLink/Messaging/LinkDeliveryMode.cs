namespace RabbitLink.Messaging
{
    /// <summary>
    /// Message delivery mode
    /// </summary>
    public enum LinkDeliveryMode : byte
    {
        /// <summary>
        /// Default
        /// </summary>
        Default = 0,

        /// <summary>
        /// Transiend
        /// </summary>
        Transient = 1,

        /// <summary>
        /// Durable
        /// </summary>
        Persistent = 2
    }
}
