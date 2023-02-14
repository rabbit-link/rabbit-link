#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Connection;
using RabbitLink.Consumer;
using RabbitLink.Messaging;
using RabbitLink.Serialization;
using RabbitLink.Topology;
using RabbitLink.Topology.Internal;

#endregion

namespace RabbitLink.Builders
{
    public delegate ILinkConsumedMessage<byte[]> DeliveryInterceptDelegate(ILinkConsumedMessage<byte[]> msg, CancellationToken ct);

    internal class LinkConsumerBuilder :
        ILinkConsumerBuilder
    {
        private readonly Link _link;
        private readonly TimeSpan _recoveryInterval;
        private readonly ushort _prefetchCount;
        private readonly bool _autoAck;
        private readonly bool _cancelOnHaFailover;
        private readonly bool _exclusive;
        private readonly int _priority;
        private readonly ILinkConsumerErrorStrategy _errorStrategy;
        private readonly LinkConsumerMessageHandlerBuilder _messageHandlerBuilder;
        private readonly ILinkConsumerTopologyHandler _topologyHandler;
        private readonly LinkStateHandler<LinkConsumerState> _stateHandler;
        private readonly LinkStateHandler<LinkChannelState> _channelStateHandler;
        private readonly ILinkSerializer _serializer;
        private readonly LinkTypeNameMapping _typeNameMapping;
        private readonly ConsumerTagProviderDelegate _consumerTagProvider;
        private readonly IReadOnlyCollection<DeliveryInterceptDelegate> _deliveryInterceptors;

        public LinkConsumerBuilder(
            Link link,
            TimeSpan recoveryInterval,
            ILinkSerializer serializer,
            ushort? prefetchCount = null,
            bool? autoAck = null,
            int? priority = null,
            bool? cancelOnHaFailover = null,
            bool? exclusive = null,
            ILinkConsumerErrorStrategy errorStrategy = null,
            LinkConsumerMessageHandlerBuilder messageHandlerBuilder = null,
            ILinkConsumerTopologyHandler topologyHandler = null,
            LinkStateHandler<LinkConsumerState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null,
            LinkTypeNameMapping typeNameMapping = null,
            ConsumerTagProviderDelegate consumerTagProvider = null,
            IReadOnlyCollection<DeliveryInterceptDelegate> deliveryInterceptors = null
        )
        {
            _link = link ?? throw new ArgumentNullException(nameof(link));

            _recoveryInterval = recoveryInterval;
            _prefetchCount = prefetchCount ?? 1;
            _autoAck = autoAck ?? false;
            _priority = priority ?? 0;
            _cancelOnHaFailover = cancelOnHaFailover ?? false;
            _exclusive = exclusive ?? false;
            _errorStrategy = errorStrategy ?? new LinkConsumerDefaultErrorStrategy();
            _messageHandlerBuilder = messageHandlerBuilder;
            _topologyHandler = topologyHandler;
            _stateHandler = stateHandler ?? ((_, _) => { });
            _channelStateHandler = channelStateHandler ?? ((_, _) => { });
            _serializer = serializer;
            _typeNameMapping = typeNameMapping ?? new LinkTypeNameMapping();
            _consumerTagProvider = consumerTagProvider;
            _deliveryInterceptors = deliveryInterceptors;
        }

        private LinkConsumerBuilder(
            LinkConsumerBuilder prev,
            TimeSpan? recoveryInterval = null,
            ushort? prefetchCount = null,
            bool? autoAck = null,
            int? priority = null,
            bool? cancelOnHaFailover = null,
            bool? exclusive = null,
            ILinkConsumerErrorStrategy errorStrategy = null,
            LinkConsumerMessageHandlerBuilder messageHandlerBuilder = null,
            ILinkConsumerTopologyHandler topologyHandler = null,
            LinkStateHandler<LinkConsumerState> stateHandler = null,
            LinkStateHandler<LinkChannelState> channelStateHandler = null,
            ILinkSerializer serializer = null,
            LinkTypeNameMapping typeNameMapping = null,
            ConsumerTagProviderDelegate consumerTagProvider = null,
            IReadOnlyCollection<DeliveryInterceptDelegate> deliveryInterceptors = null
        ) : this
        (
            prev._link,
            recoveryInterval ?? prev._recoveryInterval,
            serializer ?? prev._serializer,
            prefetchCount ?? prev._prefetchCount,
            autoAck ?? prev._autoAck,
            priority ?? prev._priority,
            cancelOnHaFailover ?? prev._cancelOnHaFailover,
            exclusive ?? prev._exclusive,
            errorStrategy ?? prev._errorStrategy,
            messageHandlerBuilder ?? prev._messageHandlerBuilder,
            topologyHandler ?? prev._topologyHandler,
            stateHandler ?? prev._stateHandler,
            channelStateHandler ?? prev._channelStateHandler,
            typeNameMapping ?? prev._typeNameMapping,
            consumerTagProvider ?? prev._consumerTagProvider,
            deliveryInterceptors ?? prev._deliveryInterceptors
        )
        {
        }


        public ILinkPullConsumerBuilder Pull
            => new LinkPullConsumerBuilder(this, _typeNameMapping, _serializer);

        public ILinkConsumer Build()
        {
            if (_topologyHandler == null)
                throw new InvalidOperationException("Queue must be set");

            if (_messageHandlerBuilder == null)
                throw new InvalidOperationException("Message handler must be set");

            if (_messageHandlerBuilder.Serializer && _serializer == null)
                throw new InvalidOperationException("Serializer needed by message handler not set");

            if (_messageHandlerBuilder.Mapping && _typeNameMapping.IsEmpty)
                throw new InvalidOperationException("Type name mapping required by handler");

            var config = new LinkConsumerConfiguration(
                _recoveryInterval,
                _prefetchCount,
                _autoAck,
                _priority,
                _cancelOnHaFailover,
                _exclusive,
                _topologyHandler,
                _stateHandler, // state handler
                _errorStrategy,
                _messageHandlerBuilder.Factory(_serializer, _typeNameMapping, _deliveryInterceptors),
                _serializer,
                _consumerTagProvider
            );

            return new LinkConsumer(config, _link.CreateChannel(_channelStateHandler, config.RecoveryInterval));
        }

        public ILinkConsumerBuilder RecoveryInterval(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than TimeSpan.Zero");

            return new LinkConsumerBuilder(this, recoveryInterval: value);
        }

        public ILinkConsumerBuilder PrefetchCount(ushort value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than 0");

            return new LinkConsumerBuilder(this, prefetchCount: value);
        }

        public ILinkConsumerBuilder AutoAck(bool value)
            => new LinkConsumerBuilder(this, autoAck: value);

        public ILinkConsumerBuilder Priority(int value)
            => new LinkConsumerBuilder(this, priority: value);

        public ILinkConsumerBuilder CancelOnHaFailover(bool value)
            => new LinkConsumerBuilder(this, cancelOnHaFailover: value);

        public ILinkConsumerBuilder Exclusive(bool value)
            => new LinkConsumerBuilder(this, exclusive: value);

        public ILinkConsumerBuilder ErrorStrategy(ILinkConsumerErrorStrategy value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkConsumerBuilder(this, errorStrategy: value);
        }

        public ILinkConsumerBuilder Handler<TBody>(LinkConsumerMessageHandlerDelegate<TBody> value) where TBody : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (typeof(TBody) == typeof(byte[]))
                return Handler(value as LinkConsumerMessageHandlerDelegate<byte[]>);

            if (typeof(TBody) == typeof(object))
                return Handler(value as LinkConsumerMessageHandlerDelegate<object>);

            return new LinkConsumerBuilder(
                this,
                messageHandlerBuilder: LinkConsumerMessageHandlerBuilder.Create(value)
            );
        }

        public ILinkConsumerBuilder Handler(LinkConsumerMessageHandlerDelegate<byte[]> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkConsumerBuilder(
                this,
                messageHandlerBuilder: LinkConsumerMessageHandlerBuilder.Create(value)
            );
        }

        public ILinkConsumerBuilder Handler(LinkConsumerMessageHandlerDelegate<object> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkConsumerBuilder(
                this,
                messageHandlerBuilder: LinkConsumerMessageHandlerBuilder.Create(value)
            );
        }

        /// <inheritdoc />
        public ILinkConsumerBuilder WithInterception(DeliveryInterceptDelegate value)
        {
            if(value == null)
                throw new ArgumentNullException(nameof(value));
            if (_deliveryInterceptors == null)
            {
                return new LinkConsumerBuilder(this, deliveryInterceptors: new[] { value });
            }

            var newInterceptors = _deliveryInterceptors.Concat(new[] { value })
                                                       .ToArray();
            return new LinkConsumerBuilder(this, deliveryInterceptors: newInterceptors);

        }

        public ILinkConsumerBuilder OnStateChange(LinkStateHandler<LinkConsumerState> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkConsumerBuilder(this, stateHandler: value);
        }

        public ILinkConsumerBuilder OnChannelStateChange(LinkStateHandler<LinkChannelState> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkConsumerBuilder(this, channelStateHandler: value);
        }

        public ILinkConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config)
        {
            return Queue(config, _ => Task.CompletedTask);
        }

        public ILinkConsumerBuilder Queue(LinkConsumerTopologyConfigDelegate config, LinkTopologyErrorDelegate error)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (error == null)
                throw new ArgumentNullException(nameof(error));

            return Queue(new LinkConsumerTopologyHandler(config, error));
        }

        public ILinkConsumerBuilder Queue(ILinkConsumerTopologyHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return new LinkConsumerBuilder(this, topologyHandler: handler);
        }

        public ILinkConsumerBuilder Serializer(ILinkSerializer value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return new LinkConsumerBuilder(this, serializer: value);
        }

        public ILinkConsumerBuilder TypeNameMap(IDictionary<Type, string> mapping)
            => TypeNameMap(map => map.Set(mapping));

        public ILinkConsumerBuilder TypeNameMap(Action<ILinkTypeNameMapBuilder> map)
        {
            var builder = new LinkTypeNameMapBuilder(_typeNameMapping);
            map?.Invoke(builder);

            return new LinkConsumerBuilder(this, typeNameMapping: builder.Build());
        }

        public ILinkConsumerBuilder ConsumerTag(ConsumerTagProviderDelegate tagProviderDelegate)
        {
            if (tagProviderDelegate == null)
                throw new ArgumentNullException(nameof(tagProviderDelegate));

            return new LinkConsumerBuilder(this, consumerTagProvider: tagProviderDelegate);
        }
    }
}
