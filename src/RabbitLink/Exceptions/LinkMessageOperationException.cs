using System;

namespace RabbitLink.Exceptions
{
    public class LinkMessageOperationException:LinkException
    {
        public LinkMessageOperationException(string message) : base(message)
        {
        }

        public LinkMessageOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
