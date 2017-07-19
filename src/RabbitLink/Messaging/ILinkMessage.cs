#region Usings

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Messaging
{
    public interface ILinkPushMessage<out T> where T : class
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
    }
    
    public interface ILinkMessage<out T> :ILinkPushMessage<T> where T : class
    {
        /// <summary>
        ///     ACK message
        /// </summary>
        Task AckAsync(CancellationToken? cancellation = null);

        /// <summary>
        ///     NACK message
        /// </summary>
        Task NackAsync(CancellationToken? cancellation = null);

        /// <summary>
        ///     Requeue message
        /// </summary>
        Task RequeueAsync(CancellationToken? cancellation = null);
    }
    
    
}