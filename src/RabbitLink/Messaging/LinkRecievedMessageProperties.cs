#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    public class LinkRecievedMessageProperties : ICloneable
    {
        public LinkRecievedMessageProperties(bool redelivered, string exchangeName, string routingKey, string queueName)
        {
            Redelivered = redelivered;
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            QueueName = queueName;
        }

        private bool Redelivered { get; }
        private string ExchangeName { get; }
        private string RoutingKey { get; }
        private string QueueName { get; }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public LinkRecievedMessageProperties Clone()
        {
            return (LinkRecievedMessageProperties) MemberwiseClone();
        }
    }
}