namespace RabbitLink.Consumer
{
    public enum LinkConsumerState
    {
        Init,
        Configuring,
        Reconfiguring,
        Active,
        Stopping,
        Disposed
    }
}