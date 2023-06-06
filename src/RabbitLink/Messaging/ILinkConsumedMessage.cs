#region Usings

using System.Threading;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Represents RabbitMQ message received from broker
    /// </summary>
    public interface ILinkConsumedMessage<out TBody> : ILinkMessage<TBody>
    {
        #region Properties

        /// <summary>
        ///     Receive properties
        /// </summary>
        LinkReceiveProperties ReceiveProperties { get; }

        /// <summary>
        ///     Message cancellation
        /// </summary>
        CancellationToken Cancellation { get; }

        #endregion
    }
}
