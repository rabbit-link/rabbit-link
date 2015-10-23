namespace RabbitLink.Exceptions
{
    public class LinkMessageReturnedException : LinkException
    {
        public LinkMessageReturnedException(string message) : base(message)
        {
        }
    }
}