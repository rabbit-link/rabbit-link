#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using RabbitLink.Connection;
using RabbitLink.Consumer;
using RabbitLink.Serialization;
using RabbitLink.Topology;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkPullConsumerBuilder : ILinkPullConsumerBuilder
    {
        #region Fields

        private readonly ILinkConsumerBuilder _consumerBuilder;
        private readonly TimeSpan _getMessageTimeout;
        private readonly LinkTypeNameMapping _typeNameMapping;
        private readonly ILinkSerializer _serializer;
        private readonly ConsumerTagProviderDelegate _consumerTagProvider;

        #endregion

        #region Ctor

        public LinkPullConsumerBuilder(
            ILinkConsumerBuilder consumerBuilder,
            LinkTypeNameMapping typeNameMapping,
            ILinkSerializer serializer,
            TimeSpan? getMessageTimeout = null,
            ConsumerTagProviderDelegate consumerTagProvider = null
        )
        {
            _serializer = serializer;
            _consumerBuilder = consumerBuilder ?? throw new ArgumentNullException(nameof(consumerBuilder));
            _getMessageTimeout = getMessageTimeout ?? Timeout.InfiniteTimeSpan;
            _typeNameMapping = typeNameMapping ?? throw new ArgumentNullException(nameof(typeNameMapping));
            _serializer = serializer;
            _consumerTagProvider = consumerTagProvider;
        }

        private LinkPullConsumerBuilder(
            LinkPullConsumerBuilder prev,
            ILinkConsumerBuilder consumerBuilder = null,
            LinkTypeNameMapping typeNameMapping = null,
            ILinkSerializer serializer = null,
            TimeSpan? getMessageTimeout = null,
            ConsumerTagProviderDelegate consumerTagProvider = null
        ) : this(
            consumerBuilder ?? prev._consumerBuilder,
            typeNameMapping ?? prev._typeNameMapping,
            serializer ?? prev._serializer,
            getMessageTimeout ?? prev._getMessageTimeout,
            consumerTagProvider ?? prev._consumerTagProvider
        )
        {
        }

        #endregion

        #region ILinkPullConsumerBuilder Members

        public ILinkPullConsumer Build()
        {
            return new LinkPullConsumer(_consumerBuilder, _getMessageTimeout, _typeNameMapping, _serializer, _consumerTagProvider);
        }

        public ILinkPullConsumerBuilder RecoveryInterval(TimeSpan value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.RecoveryInterval(value));
        }

        public ILinkPullConsumerBuilder PrefetchCount(ushort value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.PrefetchCount(value));
        }

        public ILinkPullConsumerBuilder AutoAck(bool value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.AutoAck(value));
        }

        public ILinkPullConsumerBuilder Priority(int value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Priority(value));
        }

        public ILinkPullConsumerBuilder CancelOnHaFailover(bool value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.CancelOnHaFailover(value));
        }

        public ILinkPullConsumerBuilder Exclusive(bool value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Exclusive(value));
        }

        public ILinkPullConsumerBuilder OnStateChange(LinkStateHandler<LinkConsumerState> value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.OnStateChange(value));
        }

        public ILinkPullConsumerBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> value)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.OnChannelStateChange(value));
        }

        public ILinkPullConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Queue(config));
        }

        public ILinkPullConsumerBuilder Queue(
            LinkConsumerTopologyConfigDelegate config,
            LinkTopologyErrorDelegate error
        )
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Queue(config, error));
        }

        public ILinkPullConsumerBuilder Queue(ILinkConsumerTopologyHandler handler)
        {
            return new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Queue(handler));
        }

        public ILinkPullConsumerBuilder GetMessageTimeout(TimeSpan value)
        {
            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Must be greater or equal Zero or equal Timeout.InfiniteTimeSpan");

            return new LinkPullConsumerBuilder(this, getMessageTimeout: value);
        }

        public ILinkPullConsumerBuilder Serializer(ILinkSerializer value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkPullConsumerBuilder(this, serializer: value);
        }

        public ILinkPullConsumerBuilder TypeNameMap(IDictionary<Type, string> mapping)
            => TypeNameMap(map => map.Set(mapping));

        public ILinkPullConsumerBuilder TypeNameMap(Action<ILinkTypeNameMapBuilder> map)
        {
            var builder = new LinkTypeNameMapBuilder(_typeNameMapping);
            map?.Invoke(builder);

            return new LinkPullConsumerBuilder(this, typeNameMapping: builder.Build());
        }


        public ILinkPullConsumerBuilder ConsumerTag(ConsumerTagProviderDelegate tagProviderDelegate)
        {
            if (tagProviderDelegate == null)
                throw new ArgumentNullException(nameof(tagProviderDelegate));

            return new LinkPullConsumerBuilder(this, consumerTagProvider: tagProviderDelegate);
        }

        #endregion
    }
}
