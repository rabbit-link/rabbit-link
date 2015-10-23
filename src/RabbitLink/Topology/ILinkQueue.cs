namespace RabbitLink.Topology
{
    public interface ILinkQueue
    {
        string Name { get; }
        bool IsExclusive { get; }
    }
}