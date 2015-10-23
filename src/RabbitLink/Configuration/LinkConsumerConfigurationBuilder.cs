#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Consumer;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkConsumerConfigurationBuilder :
        ILinkPullConsumerConfigurationBuilder,
        ILinkPushConsumerConfigurationBuilder
    {
        public LinkConsumerConfigurationBuilder(LinkConfiguration linkConfiguration)
        {
            Configuration.PrefetchCount = linkConfiguration.ConsumerPrefetchCount;
            Configuration.AutoAck = linkConfiguration.ConsumerAutoAck;
            Configuration.CancelOnHaFailover = linkConfiguration.ConsumerCancelOnHaFailover;
            Configuration.GetMessageTimeout = linkConfiguration.ConsumerGetMessageTimeout;
            Configuration.ErrorStrategy = linkConfiguration.ConsumerErrorStrategy;
            Configuration.MessageSerializer = linkConfiguration.MessageSerializer;
        }

        internal LinkConsumerConfiguration Configuration { get; } = new LinkConsumerConfiguration();

        public ILinkConsumerConfigurationBuilder GetMessageTimeout(TimeSpan? value)
        {
            Configuration.GetMessageTimeout = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder PrefetchCount(ushort value)
        {
            Configuration.PrefetchCount = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder AutoAck(bool value)
        {
            Configuration.AutoAck = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder Priority(int value)
        {
            Configuration.Priority = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder CancelOnHaFailover(bool value)
        {
            Configuration.CancelOnHaFailover = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder Exclusive(bool value)
        {
            Configuration.Exclusive = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder MessageSerializer(ILinkMessageSerializer value)
        {
            Configuration.MessageSerializer = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder TypeNameMap(IDictionary<Type, string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var mapping = new LinkTypeNameMapping(values);

            Configuration.TypeNameMapping.Clear();
            Configuration.TypeNameMapping.Set(mapping);

            return this;
        }

        public ILinkConsumerConfigurationBuilder TypeNameMap(Action<ILinkConfigurationTypeNameMapBuilder> map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var builder = new LinkConfigurationTypeNameMapBuilder();

            map(builder);

            Configuration.TypeNameMapping.Clear();
            Configuration.TypeNameMapping.Set(builder.Mapping);

            return this;
        }

        public ILinkConsumerConfigurationBuilder ErrorStrategy(ILinkConsumerErrorStrategy value)
        {
            Configuration.ErrorStrategy = value;
            return this;
        }
    }
}