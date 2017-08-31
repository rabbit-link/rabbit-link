#region Usings

using System.Threading;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Represents RabbitMQ message recieved from broker
    /// </summary>
    public interface ILinkConsumedMessage : ILinkMessage
    {
        #region Properties

        /// <summary>
        ///     Recieve properties
        /// </summary>
        LinkRecieveProperties RecieveProperties { get; }

        /// <summary>
        ///     Message cancellation
        /// </summary>
        CancellationToken Cancellation { get; }

        #endregion
    }
}