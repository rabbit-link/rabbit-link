namespace RabbitLink.Messaging
{
    /// <summary>
    /// Message publish properties
    /// </summary>
    public class LinkPublishProperties
    {
        private string _routingKey;

        /// <summary>
        /// Routing key
        /// </summary>
        public string RoutingKey
        {
            get => _routingKey;
            set => _routingKey = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Is message mandatory
        /// </summary>
        public bool? Mandatory { get; set; }
    }
}
