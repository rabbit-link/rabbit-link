namespace RabbitLink.Topology.Internal
{
    public enum LinkTopologyState
    {
        Init,
        Configure,
        Reconfigure,
        Ready,
        Stop,
        Dispose
    }
}