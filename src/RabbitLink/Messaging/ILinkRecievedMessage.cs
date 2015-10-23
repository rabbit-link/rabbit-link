namespace RabbitLink.Messaging
{
    public interface ILinkRecievedMessage<out T> : ILinkMessage<T> where T : class
    {
        LinkRecievedMessageProperties RecievedProperties { get; }
    }
}