namespace RabbitLink.Messaging
{
    /// <summary>
    /// Recieve properties of message
    /// </summary>
    public class LinkRecieveProperties
    {
        /// <summary>
        /// Creates new intance
        /// </summary>
        /// <param name="redelivered">Is message was redelivered</param>
        /// <param name="exchangeName">Message was published to this exchage</param>
        /// <param name="routingKey">Message was published with this routing key</param>
        /// <param name="queueName">Message was consumed from this queue</param>
        /// <param name="isFromThisApp">Message was published from this application</param>
        public LinkRecieveProperties(
            bool redelivered,
            string exchangeName,
            string routingKey,
            string queueName,
            bool isFromThisApp
        )
        {
            Redelivered = redelivered;
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            QueueName = queueName;
            IsFromThisApp = isFromThisApp;
        }

        /// <summary>
        /// Is message was redelivered
        /// </summary>
        public bool Redelivered { get; }

        /// <summary>
        /// Message was published to this exchange
        /// </summary>
        public string ExchangeName { get; }

        /// <summary>
        /// Message was published with this routing key
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        /// Message was consumed from this queue
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Message was published from this application
        /// </summary>
        public bool IsFromThisApp { get; }
    }
}
