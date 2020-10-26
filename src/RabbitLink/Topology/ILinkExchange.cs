namespace RabbitLink.Topology
{
    /// <summary>
    /// Represents RabbitMQ exchange
    /// </summary>
    public interface ILinkExchange
    {
        /// <summary>
        /// Name of exchange
        /// </summary>
        string Name { get; }
    }
}
