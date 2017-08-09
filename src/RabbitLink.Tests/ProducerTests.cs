#region Usings

using System;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;
using RabbitLink.Producer;
using RabbitLink.Topology;
using Xunit;

#endregion

namespace RabbitLink.Tests
{
    public class ProducerTests
    {
        [Fact]
        public void PublishConfirms()
        {
            var exchangeName = TestsOptions.TestExchangeName;
            var queueName = TestsOptions.TestQueueName;

            using (var link = TestsOptions.GetLinkBuilder().Build())
            {
                try
                {
                    using (var producer = link.Producer
                        .Queue(async cfg =>
                        {
                            var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                            var q = await cfg.QueueDeclare(queueName, true, false, true,
                                expires: TimeSpan.FromMinutes(1));

                            await cfg.Bind(q, ex);

                            return ex;
                        })
                        .ConfirmsMode(true)
                        .Build()
                    )
                    {
                        producer.PublishAsync(new LinkPublishMessage(new byte[0]))
                            .GetAwaiter()
                            .GetResult();
                    }
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                        {
                            try
                            {
                                var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                                var q = await cfg.QueueDeclarePassive(queueName);

                                await cfg.ExchangeDelete(ex);
                                await cfg.QueueDelete(q);
                            }
                            catch
                            {
                                // No-op
                            }
                        })
                        .GetAwaiter()
                        .GetResult();
                }
            }
        }

        [Fact]
        public void PublishConfirmsMandatory()
        {
            var exchangeName = TestsOptions.TestExchangeName;
            var queueName = TestsOptions.TestQueueName;

            using (var link = TestsOptions.GetLinkBuilder().Build())
            {
                try
                {
                    using (var producer = link.Producer
                        .Queue(async cfg =>
                        {
                            var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                            var q = await cfg.QueueDeclare(queueName, true, false, true,
                                expires: TimeSpan.FromMinutes(1));

                            await cfg.Bind(q, ex);

                            return ex;
                        })
                        .ConfirmsMode(true)
                        .Build()
                    )
                    {
                        producer.PublishAsync(new LinkPublishMessage(
                                new byte[0],
                                publishProperties: new LinkPublishProperties {Mandatory = true}
                            ))
                            .GetAwaiter()
                            .GetResult();
                    }
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                        {
                            try
                            {
                                var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                                var q = await cfg.QueueDeclarePassive(queueName);

                                await cfg.QueueDelete(q);
                                await cfg.ExchangeDelete(ex);
                            }
                            catch
                            {
                                // No-op
                            }
                        })
                        .GetAwaiter()
                        .GetResult();
                }

                try
                {
                    using (var producer = link.Producer
                        .Queue(async cfg =>
                        {
                            var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);

                            return ex;
                        })
                        .ConfirmsMode(true)
                        .Build())
                    {
                        Assert.Throws<LinkMessageReturnedException>(() =>
                        {
                            producer.PublishAsync(new LinkPublishMessage(
                                    new byte[0],
                                    publishProperties: new LinkPublishProperties
                                    {
                                        Mandatory = true
                                    }
                                ))
                                .GetAwaiter()
                                .GetResult();
                        });
                    }
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                        {
                            try
                            {
                                var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                                await cfg.ExchangeDelete(ex);
                            }
                            catch
                            {
                                // No-op
                            }
                        })
                        .GetAwaiter()
                        .GetResult();
                }
            }
        }

        [Fact]
        public void PublishTest()
        {
            var exchangeName = TestsOptions.TestExchangeName;
            var queueName = TestsOptions.TestQueueName;

            using (var link = TestsOptions.GetLinkBuilder().Build())
            {
                try
                {
                    using (var producer = link.Producer
                        .Queue(async cfg =>
                        {
                            var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                            var q = await cfg.QueueDeclare(queueName, false, false, true,
                                expires: TimeSpan.FromMinutes(1));

                            await cfg.Bind(q, ex);

                            return ex;
                        })
                        .ConfirmsMode(false)
                        .Build()
                    )
                    {
                        producer.PublishAsync(new LinkPublishMessage(new byte[0]))
                            .GetAwaiter()
                            .GetResult();
                    }
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                        {
                            try
                            {
                                var ex = await cfg.ExchangeDeclarePassive(exchangeName);

                                var q = await cfg.QueueDeclarePassive(queueName);

                                await cfg.ExchangeDelete(ex);
                                await cfg.QueueDelete(q);
                            }
                            catch
                            {
                                // No-op
                            }
                        })
                        .GetAwaiter()
                        .GetResult();
                }
            }
        }

        [Fact]
        public void PublishTimeoutTest()
        {
            var exchangeName = TestsOptions.TestExchangeName;

            using (var link = TestsOptions.GetLinkBuilder().Build())
            {
                try
                {
                    using (var producer = link.Producer
                        .Queue(async cfg =>
                        {
                            var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);

                            return ex;
                        })
                        .ConfirmsMode(false)
                        .Build()
                    )
                    {
                        Assert.ThrowsAny<OperationCanceledException>(
                            () =>
                            {
                                producer.PublishAsync(new byte[0], TimeSpan.Zero)
                                    .GetAwaiter()
                                    .GetResult();
                            });
                    }
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                        {
                            try
                            {
                                var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                                await cfg.ExchangeDelete(ex);
                            }
                            catch
                            {
                                // No-op
                            }
                        })
                        .GetAwaiter()
                        .GetResult();
                }
            }
        }
    }
}