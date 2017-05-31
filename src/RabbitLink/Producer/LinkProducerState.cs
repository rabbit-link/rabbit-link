namespace RabbitLink.Producer
{
    public enum LinkProducerState
    {
        Init,
        Configure,
        Reconfigure,
        Active,
        Stop,
        Dispose
    }
}