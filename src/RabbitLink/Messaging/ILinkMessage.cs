namespace RabbitLink.Messaging
{
    public interface ILinkMessage<out T> where T : class
    {
        /// <summary>
        ///     The message properties.
        /// </summary>
        LinkMessageProperties Properties { get; }

        /// <summary>
        ///     The message body as a .NET type.
        /// </summary>
        T Body { get; }
    }
}