#region Usings

using System;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Builders
{
    internal struct LinkProducerConfiguration
    {
        public LinkMessageProperties MessageProperties;
        public LinkPublishProperties PublishProperties;
        public TimeSpan PublishTimeout;
        public TimeSpan RecoveryInterval;
        public ILinkMessageIdGenerator MessageIdGenerator;
        public bool ConfigrmsMode;
        public bool SetUserId;
    }
}