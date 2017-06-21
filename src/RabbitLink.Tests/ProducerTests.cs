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

            using (var link = new Link(TestsOptions.ConnectionString))
            {
                try
                {
                    using (var producer = link.CreateProducer(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                        var q = await cfg.QueueDeclare(queueName, true, false, true, expires: TimeSpan.FromMinutes(1));

                        await cfg.Bind(q, ex);

                        return ex;
                    }, config: cfg => cfg.ConfirmsMode(true)))
                    {
                        producer.PublishAsync(new byte[] { })
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

            using (var link = new Link(TestsOptions.ConnectionString))
            {
                try
                {
                    using (var producer = link.CreateProducer(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                        var q = await cfg.QueueDeclare(queueName, true, false, true, expires: TimeSpan.FromMinutes(1));

                        await cfg.Bind(q, ex);

                        return ex;
                    }, config: cfg => cfg.ConfirmsMode(true)))
                    {
                        producer.PublishAsync(new byte[] { }, publishProperties: new LinkPublishProperties
                            {
                                Mandatory = true
                            })
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
                    using (var producer = link.CreateProducer(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);

                        return ex;
                    }, config: cfg => cfg.ConfirmsMode(true)))
                    {
                        Assert.Throws<LinkMessageReturnedException>(() =>
                        {
                            producer.PublishAsync(new byte[] { }, publishProperties: new LinkPublishProperties
                                {
                                    Mandatory = true
                                })
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

            using (var link = new Link(TestsOptions.ConnectionString))
            {
                try
                {
                    using (var producer = link.CreateProducer(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                        var q = await cfg.QueueDeclare(queueName, false, false, true, expires: TimeSpan.FromMinutes(1));

                        await cfg.Bind(q, ex);

                        return ex;
                    }, config: cfg => cfg.ConfirmsMode(false)))
                    {
                        producer.PublishAsync(new byte[] { })
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

            using (var link = new Link(TestsOptions.ConnectionString))
            {
                try
                {
                    using (var producer = link.CreateProducer(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);

                        return ex;
                    }, config: cfg => cfg.ConfirmsMode(false)))
                    {
                        Assert.ThrowsAny<OperationCanceledException>(
                            () =>
                            {
                                producer.PublishAsync(new byte[] {}, TimeSpan.Zero)
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