#region Usings

using System;
using System.Threading;

#endregion

namespace RabbitLink.Messaging.Internals
{
    /// <summary>
    ///     Represents RabbitMQ message received from broker
    /// </summary>
    internal class LinkConsumedMessage<TBody> : LinkMessage<TBody>, ILinkConsumedMessage<TBody> 
    {
        /// <summary>
        ///     Creates instance
        /// </summary>
        /// <param name="body">Body value</param>
        /// <param name="properties">Message properties</param>
        /// <param name="receiveProperties">Receive properties</param>
        /// <param name="cancellation">Message cancellation</param>
        public LinkConsumedMessage(
            TBody body,
            LinkMessageProperties properties,
            LinkReceiveProperties receiveProperties,
            CancellationToken cancellation
        ) : base(
            body,
            properties
        )
        {
            ReceiveProperties = receiveProperties ?? throw new ArgumentNullException(nameof(receiveProperties));
            Cancellation = cancellation;
        }

        /// <summary>
        ///     Receive properties
        /// </summary>
        public LinkReceiveProperties ReceiveProperties { get; }

        /// <summary>
        ///     Message cancellation
        /// </summary>
        public CancellationToken Cancellation { get; }
    }
}
