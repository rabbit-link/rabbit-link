namespace RabbitLink.Exceptions
{
    public class LinkNotConnectedException : LinkException
    {
        public LinkNotConnectedException() : base("Not connected")
        {
        }
    }
}