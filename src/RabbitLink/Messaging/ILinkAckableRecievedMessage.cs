namespace RabbitLink.Messaging
{
    public interface ILinkAckableRecievedMessage<out T> : ILinkRecievedMessage<T>
        where T : class
    {
        bool Acked { get; }
        bool Nacked { get; }
        void Ack();
        void Nack(bool requeue = false);
    }
}