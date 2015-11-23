#region Usings

using System;
using RabbitLink.Configuration;

#endregion

namespace RabbitLink.Messaging
{
    public class LinkRecievedMessageProperties : ICloneable
    {
        public LinkRecievedMessageProperties(bool redelivered, string exchangeName, string routingKey, string queueName, bool isFromThisApp)
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
        /// Message was published from this application ( <see cref="ILinkConfigurationBuilder.AppId"/> == <see cref="LinkMessageProperties.AppId" /> )
        /// </summary>
        public bool IsFromThisApp { get; }

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