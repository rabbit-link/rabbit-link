#region Usings

using System;

#endregion

namespace RabbitLink.Exceptions
{
    public class LinkException : Exception
    {
        public LinkException(string message) : base(message)
        {
        }

        public LinkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}