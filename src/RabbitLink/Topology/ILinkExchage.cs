namespace RabbitLink.Topology
{
    /// <summary>
    /// Represents RabbitMQ exchage
    /// </summary>
    public interface ILinkExchage
    {
        /// <summary>
        /// Name of exchange
        /// </summary>
        string Name { get; }
    }
}
