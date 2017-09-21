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

        #endregion

        #region Ctor

        public LinkPullConsumerBuilder(
            ILinkConsumerBuilder consumerBuilder,
            TimeSpan? getMessageTimeout = null,
            LinkTypeNameMapping typeNameMapping = null
        )
        {
            _consumerBuilder = consumerBuilder ?? throw new ArgumentNullException(nameof(consumerBuilder));
            _getMessageTimeout = getMessageTimeout ?? Timeout.InfiniteTimeSpan;
            _typeNameMapping = typeNameMapping ?? new LinkTypeNameMapping();
        }

        private LinkPullConsumerBuilder(
            LinkPullConsumerBuilder prev,
            ILinkConsumerBuilder consumerBuilder = null,
            TimeSpan? getMessageTimeout = null,
            LinkTypeNameMapping typeNameMapping = null
        ) : this(
            consumerBuilder ?? prev._consumerBuilder,
            getMessageTimeout ?? prev._getMessageTimeout,
            typeNameMapping ?? prev._typeNameMapping
        )
        {
        }

        #endregion

        #region ILinkPullConsumerBuilder Members

        public ILinkPullConsumer Build()
        {
            return new LinkPullConsumer(_consumerBuilder, _getMessageTimeout, _typeNameMapping);
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

        public ILinkPullConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config,
            LinkTopologyErrorDelegate error)
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
            => new LinkPullConsumerBuilder(this, consumerBuilder: _consumerBuilder.Serializer(value));

        public ILinkPullConsumerBuilder TypeNameMap(IDictionary<Type, string> mapping)
            => TypeNameMap(map => map.Set(mapping));

        public ILinkPullConsumerBuilder TypeNameMap(Action<ILinkTypeNameMapBuilder> map)
        {
            var builder = new LinkTypeNameMapBuilder(_typeNameMapping);
            map?.Invoke(builder);

            return new LinkPullConsumerBuilder(this, typeNameMapping: builder.Build());
        }

        #endregion
    }
}
