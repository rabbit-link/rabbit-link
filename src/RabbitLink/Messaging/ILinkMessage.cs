#region Usings

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Messaging
{
    public interface ILinkMessage<out T> where T : class
    {
        /// <summary>
        ///     The message properties.
        /// </summary>
        LinkMessageProperties Properties { get; }

        /// <summary>
        ///     The message recieve properties
        /// </summary>
        LinkRecieveMessageProperties RecieveProperties { get; }

        /// <summary>
        ///     The message body as a .NET type.
        /// </summary>
        T Body { get; }        

        /// <summary>
        ///     ACK message
        /// </summary>
        Task AckAsync(CancellationToken? cancellation = null);

        /// <summary>
        ///     NACK message
        /// </summary>
        Task Nack(CancellationToken? cancellation = null);

        /// <summary>
        ///     Requeue message
        /// </summary>
        Task Requeue(CancellationToken? cancellation = null);
    }
}