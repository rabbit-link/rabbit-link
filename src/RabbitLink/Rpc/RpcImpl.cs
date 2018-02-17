using System;
using System.Threading.Tasks;
using RabbitLink.Consumer;
using RabbitLink.Exceptions;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;
using RabbitLink.Producer;
using RabbitLink.Serialization;
using RabbitMQ.Client.Impl;

namespace RabbitLink.Rpc
{
    internal static class RpcImpl
    {
        public static LinkConsumerMessageHandlerDelegate<byte[]> Serve<TOut>(
            ILinkProducer replayChannel, ILinkLogger logger, bool strictTwoWay,
            LinkServeHandlerDelegate<byte[], TOut> handler)
            where TOut : class
            => async msg =>
            {
                var needReplay = !string.IsNullOrWhiteSpace(msg.Properties.ReplyTo);
                if (!needReplay && strictTwoWay)
                {
                    logger.Write(LinkLoggerLevel.Warning, $"Message without ReplyTo from {msg.Properties.AppId}");
                    return LinkConsumerAckStrategy.Nack;
                }

                ILinkPublishMessage<TOut> response;
                try
                {
                    response = await handler(msg);
                }
                catch (LinkRetryAttemptException rex)
                {
                    logger.Write(LinkLoggerLevel.Info, $"Retrying serve in {rex.RetryAfter}");
                    await Task.Delay(rex.RetryAfter, msg.Cancellation);
                    return LinkConsumerAckStrategy.Requeue;
                }
                catch (Exception ex)
                {
                    logger.Write(LinkLoggerLevel.Error, $"Error serve message {ex}");
                    if (!needReplay) return LinkConsumerAckStrategy.Nack;
                    try
                    {
                        await replayChannel.PublishAsync(new LinkPublishMessage<Exception>(ex,
                            new LinkMessageProperties
                            {
                                CorrelationId = msg.Properties.CorrelationId
                            }, new LinkPublishProperties
                            {
                                RoutingKey = msg.Properties.ReplyTo
                            }), msg.Cancellation);
                        return LinkConsumerAckStrategy.Ack;
                    }
                    catch (Exception pex)
                    {
                        logger.Write(LinkLoggerLevel.Error, $"Error send error response {pex}");
                        return LinkConsumerAckStrategy.Nack;
                    }
                }

                if (!needReplay) return LinkConsumerAckStrategy.Ack;

                if (string.IsNullOrWhiteSpace(response.Properties.CorrelationId))
                    response.Properties.CorrelationId = msg.Properties.CorrelationId;
                if (string.IsNullOrWhiteSpace(response.PublishProperties.RoutingKey))
                    response.PublishProperties.RoutingKey = msg.Properties.ReplyTo;


                await replayChannel.PublishAsync(response, msg.Cancellation);
                return LinkConsumerAckStrategy.Ack;
            };


        private static LinkServeHandlerDelegate<byte[], TOut> ServeHadler<TIn, TOut>(ILinkSerializer serializer,
            LinkServeHandlerDelegate<TIn, TOut> handler)
            where TOut : class
            where TIn : class
            => async msg =>
            {
                var request = serializer.Deserialize<TIn>(msg.Body, msg.Properties);

                var requestMsg = new LinkConsumedMessage<TIn>(request, msg.Properties, msg.RecieveProperties, msg.Cancellation);

                return await handler(requestMsg);
            };

        public static LinkConsumerMessageHandlerDelegate<byte[]> Serve<TIn, TOut>(
            ILinkProducer replayChannel, ILinkLogger logger, ILinkSerializer serializer, bool strictTwoWay,
            LinkServeHandlerDelegate<TIn, TOut> handler)
            where TOut : class
            where TIn : class
            => Serve(replayChannel, logger, strictTwoWay, ServeHadler(serializer, handler));

        private static readonly Task<LinkConsumerAckStrategy> NackTask = Task.FromResult(LinkConsumerAckStrategy.Nack);
        private static readonly Task<LinkConsumerAckStrategy> AckTask = Task.FromResult(LinkConsumerAckStrategy.Ack);

        public static LinkConsumerMessageHandlerDelegate<byte[]> ReplayHandler(CorrelationDictonary dictionary)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            return msg =>
            {
                var correlationId = msg.Properties.CorrelationId;
                if (string.IsNullOrWhiteSpace(correlationId))
                    return NackTask;
                if (!dictionary.TryRemove(correlationId, out var source))
                    return NackTask;
                return source.TrySetResult(msg) ? AckTask : NackTask;
            };
        }

    }
}
