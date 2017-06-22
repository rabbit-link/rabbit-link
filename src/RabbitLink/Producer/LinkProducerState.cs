namespace RabbitLink.Producer
{
    public enum LinkProducerState
    {
        Init,
        Configuring,
        Reconfiguring,
        Active,
        Stopping,
        Disposed
    }
}