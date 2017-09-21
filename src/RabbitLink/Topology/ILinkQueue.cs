namespace RabbitLink.Topology
{
    /// <summary>
    /// Represents queue in RabbitMQ
    /// </summary>
    public interface ILinkQueue
    {
        /// <summary>
        /// Name of queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Is queue exclusive (may be used by only one consumer)
        /// </summary>
        bool IsExclusive { get; }
    }
}
