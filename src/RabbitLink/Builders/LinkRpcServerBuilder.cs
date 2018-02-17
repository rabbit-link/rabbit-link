using System;
using RabbitLink.Consumer;
using RabbitLink.Logging;
using RabbitLink.Producer;
using RabbitLink.Rpc;
using RabbitLink.Serialization;

namespace RabbitLink.Builders
{
    internal class LinkRpcServerBuilder : ILinkRpcServerBuilder
    {
        private readonly ILinkConsumerBuilder _consumerBuilder;
        private readonly ILinkProducer _replayProducer;
        private readonly ILinkLogger _logger;
        private readonly ILinkSerializer _serializer;
        private readonly bool _strictTwoWay;

        private readonly Func<ILinkProducer, ILinkLogger, ILinkSerializer, bool,
            LinkConsumerMessageHandlerDelegate<byte[]>> _handler;


        public LinkRpcServerBuilder(ILinkConsumerBuilder consumerBuilder, ILinkProducer replayProducer,
            ILinkLogger logger, ILinkSerializer serializer, bool strictTwoWay,
            Func<ILinkProducer, ILinkLogger, ILinkSerializer, bool, LinkConsumerMessageHandlerDelegate<byte[]>> handler)
        {
            _consumerBuilder = consumerBuilder;
            _replayProducer = replayProducer;
            _logger = logger;
            _serializer = serializer;
            _strictTwoWay = strictTwoWay;
            _handler = handler;
        }

        public ILinkConsumer Build()
        {
            if(_handler == null)
                throw new InvalidOperationException("Rpc handler not specified!");
            return _consumerBuilder
                .Handler(_handler(_replayProducer, _logger, _serializer, _strictTwoWay))
                .Build();
        }

        public ILinkRpcServerBuilder StrictTwoWay(bool strictTwoWay)
        {
            return new LinkRpcServerBuilder(_consumerBuilder, _replayProducer, _logger, _serializer, strictTwoWay, _handler);
        }

        public ILinkRpcServerBuilder Handler<TOut>(LinkServeHandlerDelegate<byte[], TOut> handler)
            where TOut : class
        {
            return new LinkRpcServerBuilder(_consumerBuilder, _replayProducer, _logger, _serializer,
                _strictTwoWay,
                (producer, logger, serailizer, twoWay) =>
                    RpcImpl.Serve(producer, logger, twoWay, handler));
        }

        public ILinkRpcServerBuilder Handler<TIn, TOut>(LinkServeHandlerDelegate<TIn, TOut> handler)
            where TOut : class
            where TIn : class
        {
            if(_serializer == null)
                throw new InvalidOperationException("Serializer for typed rpc handler not specified");
            return new LinkRpcServerBuilder(_consumerBuilder, _replayProducer, _logger, _serializer,
                _strictTwoWay,
                (producer, logger, serailizer, twoWay) =>
                    RpcImpl.Serve(producer, logger, serailizer, twoWay, handler));
        }
    }
}
