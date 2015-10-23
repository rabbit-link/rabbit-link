namespace RabbitLink.Exceptions
{
    public class LinkMessageNackedException : LinkException
    {
        public LinkMessageNackedException() : base("Message NACKed")
        {
        }
    }
}