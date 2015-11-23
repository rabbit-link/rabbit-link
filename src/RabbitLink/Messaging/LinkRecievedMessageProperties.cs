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

        public bool Redelivered { get; }
        public string ExchangeName { get; }
        public string RoutingKey { get; }
        public string QueueName { get; }

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