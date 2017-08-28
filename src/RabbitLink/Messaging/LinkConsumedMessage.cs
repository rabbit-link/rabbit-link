using System;
using System.Threading;

namespace RabbitLink.Messaging
{
    /// <summary>
    /// Message recieved from RabbitMQ
    /// </summary>
    public class LinkConsumedMessage : LinkMessage
    {
        /// <summary>
        /// Creates intance
        /// </summary>
        /// <param name="body">Body value</param>
        /// <param name="properties">Message properties</param>
        /// <param name="recieveProperties">Recieve properties</param>
        /// <param name="cancellation">Message cancellation</param>
        public LinkConsumedMessage(
            byte[] body,
            LinkMessageProperties properties,
            LinkRecieveProperties recieveProperties,
            CancellationToken cancellation
        ) : base(
            body,
            properties
        )
        {
            RecieveProperties = recieveProperties ?? throw new ArgumentNullException(nameof(recieveProperties));
            Cancellation = cancellation;
        }

        /// <summary>
        /// Recieve properties
        /// </summary>
        public LinkRecieveProperties RecieveProperties { get; }
        
        /// <summary>
        /// Message cancellation
        /// </summary>
        public CancellationToken Cancellation { get; }
    }
}