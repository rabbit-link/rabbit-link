#region Usings

using System;

#endregion

namespace RabbitLink.Exceptions
{
    public class LinkSerializationException : LinkException
    {
        public LinkSerializationException(Exception innerException)
            : base("Cannot serialize message, see InnerException for details", innerException)
        {
        }
    }
}