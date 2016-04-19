#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkProducerConfigurationBuilder : ILinkProducerConfigurationBuilder
    {
        public LinkProducerConfigurationBuilder(LinkConfiguration linkConfiguration)
        {
            Configuration.ConfirmsMode = linkConfiguration.ProducerConfirmsMode;
            Configuration.MessageProperties = linkConfiguration.ProducerMessageProperties;
            Configuration.PublishTimeout = linkConfiguration.ProducerPublishTimeout;
            Configuration.MessageSerializer = linkConfiguration.MessageSerializer;
            Configuration.SetUserId = linkConfiguration.ProducerSetUserId;
            Configuration.MessageIdGenerator = linkConfiguration.ProducerMessageIdGenerator;
        }

        internal LinkProducerConfiguration Configuration { get; } = new LinkProducerConfiguration();

        public ILinkProducerConfigurationBuilder ConfirmsMode(bool value)
        {
            Configuration.ConfirmsMode = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder MessageProperties(LinkMessageProperties value)
        {
            Configuration.MessageProperties = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder PublishProperties(LinkPublishProperties value)
        {
            Configuration.PublishProperties = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder PublishTimeout(TimeSpan? value)
        {
            Configuration.PublishTimeout = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder MessageSerializer(ILinkMessageSerializer value)
        {
            Configuration.MessageSerializer = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder SetUserId(bool value)
        {
            Configuration.SetUserId = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder MessageIdGenerator(ILinkMessageIdGenerator value)
        {
            Configuration.MessageIdGenerator = value;
            return this;
        }

        public ILinkProducerConfigurationBuilder TypeNameMap(IDictionary<Type, string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var mapping = new LinkTypeNameMapping(values);

            Configuration.TypeNameMapping.Clear();
            Configuration.TypeNameMapping.Set(mapping);

            return this;
        }

        public ILinkProducerConfigurationBuilder TypeNameMap(Action<ILinkConfigurationTypeNameMapBuilder> map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var builder = new LinkConfigurationTypeNameMapBuilder();

            map(builder);

            Configuration.TypeNameMapping.Clear();
            Configuration.TypeNameMapping.Set(builder.Mapping);

            return this;
        }
    }
}