#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Messaging;
using RabbitLink.Producer;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkProducerBuilder : ILinkProducerConfigurationBuilder
    {
        public LinkProducerBuilder
        (
        )
        {
           
        }

        internal LinkProducerConfiguration Configuration { get; } = new LinkProducerConfiguration();

        public ILinkProducerConfigurationBuilder ConfirmsMode(bool value)
        {
            //Configuration.ConfirmsMode = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder MessageProperties(LinkMessageProperties value)
        {
            //Configuration.MessageProperties = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder PublishProperties(LinkPublishProperties value)
        {
            ///Configuration.PublishProperties = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder PublishTimeout(TimeSpan? value)
        {
            //Configuration.PublishTimeout = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder SetUserId(bool value)
        {
            //Configuration.SetUserId = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder MessageIdGenerator(ILinkMessageIdGenerator value)
        {
            //Configuration.MessageIdGenerator = value;
            return this;
        }

        public ILinkProducer Build()
        {
            throw new NotImplementedException();
        }
    }
}