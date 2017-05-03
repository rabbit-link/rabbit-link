#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Connection;
using RabbitLink.Logging;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkTopologyConfig : ILinkTopologyConfig
    {
        private readonly ILinkChannel _channel;
        private readonly ILinkLogger _logger;

        public LinkTopologyConfig(ILinkLogger logger, ILinkChannel channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _channel = channel;
            _logger = logger;
        }

        public async Task Bind(ILinkExchage destination, ILinkExchage source, string routingKey = null,
            IDictionary<string, object> arguments = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (routingKey == null)
                routingKey = string.Empty;

            if (arguments == null)
                arguments = new Dictionary<string, object>();

            await
                _channel.InvokeActionAsync(
                    model => model.ExchangeBind(destination.Name, source.Name, routingKey, arguments), CancellationToken.None)
                    .ConfigureAwait(false);

            _logger.Debug(
                $"Bound destination exchange {destination.Name} to source exchange {source.Name} with routing key {routingKey} and arguments: {string.Join(", ", arguments.Select(x => $"{x.Key} = {x.Value}"))}");
        }

        public async Task Bind(ILinkQueue queue, ILinkExchage exchange, string routingKey = null,
            IDictionary<string, object> arguments = null)
        {
            if (exchange == null)
                throw new ArgumentNullException(nameof(exchange));

            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            if (routingKey == null)
                routingKey = string.Empty;

            if (arguments == null)
                arguments = new Dictionary<string, object>();

            await _channel.InvokeActionAsync(model => model.QueueBind(queue.Name, exchange.Name, routingKey, arguments), CancellationToken.None)
                .ConfigureAwait(false);

            _logger.Debug(
                $"Bound queue {queue.Name} to exchange {exchange.Name} with routing key {routingKey} and arguments: {string.Join(", ", arguments.Select(x => $"{x.Key} = {x.Value}"))}");
        }

        #region Exchange

        public async Task<ILinkExchage> ExchangeDeclare(
            string name,
            LinkExchangeType type,
            bool durable = true,
            bool autoDelete = false,
            string alternateExchange = null,
            bool delayed = false
            )
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            string exchangeType;
            switch (type)
            {
                case LinkExchangeType.Direct:
                    exchangeType = ExchangeType.Direct;
                    break;
                case LinkExchangeType.Fanout:
                    exchangeType = ExchangeType.Fanout;
                    break;
                case LinkExchangeType.Headers:
                    exchangeType = ExchangeType.Headers;
                    break;
                case LinkExchangeType.Topic:
                    exchangeType = ExchangeType.Topic;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            var arguments = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(alternateExchange))
            {
                arguments.Add("alternate-exchange", alternateExchange);
            }

            if (delayed)
            {
                arguments.Add("x-delayed-type", exchangeType);
                exchangeType = "x-delayed-message";
            }

            await
                _channel.InvokeActionAsync(
                    model => model.ExchangeDeclare(name, exchangeType, durable, autoDelete, arguments), CancellationToken.None)
                    .ConfigureAwait(false);

            _logger.Debug(
                $"Declared exchange \"{name}\", type: {exchangeType}, durable: {durable}, autoDelete: {autoDelete}, arguments: {string.Join(", ", arguments.Select(x => $"{x.Key} = {x.Value}"))}");

            return new LinkExchage(name);
        }

        public async Task<ILinkExchage> ExchangeDeclarePassive(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            await _channel.InvokeActionAsync(model => model.ExchangeDeclarePassive(name), CancellationToken.None)
                .ConfigureAwait(false);

            _logger.Debug($"Declared exchange passive: \"{name}\"");

            return new LinkExchage(name);
        }

        public Task<ILinkExchage> ExchangeDeclareDefault()
        {
            _logger.Debug($"Declared default exchange");

            return Task.FromResult((ILinkExchage) new LinkExchage(""));
        }

        public async Task ExchangeDelete(ILinkExchage exchange, bool ifUnused = false)
        {
            if (exchange == null)
                throw new ArgumentNullException(nameof(exchange));

            await _channel.InvokeActionAsync(model => model.ExchangeDelete(exchange.Name, ifUnused), CancellationToken.None)
                .ConfigureAwait(false);

            _logger.Debug($"Deleted exchange \"{exchange.Name}\", unused: {ifUnused}");
        }

        #endregion

        #region Queue

        public async Task<ILinkQueue> QueueDeclareExclusiveByServer()
        {
            string name = null;

            await _channel.InvokeActionAsync(model =>
            {
                var resule = model.QueueDeclare();
                name = resule.QueueName;
            }, CancellationToken.None)
                .ConfigureAwait(false);

            _logger.Debug($"Declared exclusive queue with name from server: \"{name}\"");

            return new LinkQueue(name, true);
        }

        public async Task<ILinkQueue> QueueDeclareExclusive(
            bool autoDelete = true,
            TimeSpan? messageTtl = null,
            TimeSpan? expires = null,
            byte? maxPriority = null,
            int? maxLength = null,
            int? maxLengthBytes = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null
            )
        {
            return await QueueDeclare(
                $"exclusive-{Guid.NewGuid():N}", false, true, autoDelete, messageTtl, expires, maxPriority, maxLength,
                maxLengthBytes, deadLetterExchange, deadLetterRoutingKey
                );
        }

        public async Task<ILinkQueue> QueueDeclareExclusive(
            string prefix,
            bool autoDelete = true,
            TimeSpan? messageTtl = null,
            TimeSpan? expires = null,
            byte? maxPriority = null,
            int? maxLength = null,
            int? maxLengthBytes = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null
            )
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(nameof(prefix));

            return await QueueDeclare(
                $"{prefix}-exclusive-{Guid.NewGuid():N}", false, true, autoDelete, messageTtl, expires, maxPriority,
                maxLength, maxLengthBytes, deadLetterExchange, deadLetterRoutingKey
                );
        }

        public async Task<ILinkQueue> QueueDeclarePassive(string name)
        {
            await _channel.InvokeActionAsync(model => model.QueueDeclarePassive(name), CancellationToken.None)
                .ConfigureAwait(false);

            return new LinkQueue(name, false);
        }

        public async Task<ILinkQueue> QueueDeclare(
            string name,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            TimeSpan? messageTtl = null,
            TimeSpan? expires = null,
            byte? maxPriority = null,
            int? maxLength = null,
            int? maxLengthBytes = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null
            )
        {
            var arguments = new Dictionary<string, object>();

            if (messageTtl != null)
            {
                if (messageTtl.Value.TotalMilliseconds < 0 || messageTtl.Value.TotalMilliseconds > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(messageTtl),
                        "Must be greater or equal 0 and less than Int32.MaxValue");

                arguments.Add("x-message-ttl", (int) messageTtl.Value.TotalMilliseconds);
            }

            if (expires != null)
            {
                if(expires.Value.TotalMilliseconds<= 0 || expires.Value.TotalMilliseconds > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(expires), "Total milliseconds must be greater than 0 and less than Int32.MaxValue");

                arguments.Add("x-expires", (int)expires.Value.TotalMilliseconds);
            }

            if (maxPriority != null)
            {
                arguments.Add("x-max-priority", maxPriority.Value);
            }

            if (maxLength != null)
            {
                if (maxLength <= 0)
                    throw new ArgumentOutOfRangeException(nameof(maxLength), "Must be greater than 0");

                arguments.Add("x-max-length", maxLength.Value);
            }

            if (maxLengthBytes != null)
            {
                if (maxLengthBytes <= 0)
                    throw new ArgumentOutOfRangeException(nameof(maxLengthBytes), "Must be greater than 0");

                arguments.Add("x-max-length-bytes", maxLengthBytes.Value);
            }

            if (deadLetterExchange != null)
            {
                arguments.Add("x-dead-letter-exchange", deadLetterExchange);
            }

            if (deadLetterRoutingKey != null)
            {
                arguments.Add("x-dead-letter-routing-key", deadLetterRoutingKey);
            }

            await
                _channel.InvokeActionAsync(model => model.QueueDeclare(name, durable, exclusive, autoDelete, arguments), CancellationToken.None)
                    .ConfigureAwait(false);

            _logger.Debug(
                $"Declared queue \"{name}\", durable: {durable}, exclusive: {exclusive}, autoDelete: {autoDelete}, arguments: {string.Join(", ", arguments.Select(x => $"{x.Key} = {x.Value}"))}");

            return new LinkQueue(name, exclusive);
        }

        public async Task QueueDelete(ILinkQueue queue, bool ifUnused = false, bool ifEmpty = false)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            await _channel.InvokeActionAsync(model => model.QueueDelete(queue.Name, ifUnused, ifEmpty), CancellationToken.None)
                .ConfigureAwait(false);

            _logger.Debug($"Deleted queue \"{queue.Name}\", unused: {ifUnused}, empty: {ifEmpty}");
        }

        public async Task QueuePurge(ILinkQueue queue)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            await _channel.InvokeActionAsync(model => model.QueuePurge(queue.Name), CancellationToken.None)
                .ConfigureAwait(false);

            _logger.Debug($"Purged queue \"{queue.Name}\"");
        }

        #endregion
    }
}