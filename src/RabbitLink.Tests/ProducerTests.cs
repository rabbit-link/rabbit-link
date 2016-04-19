#region Usings

using System;
using System.Threading.Tasks;
using Nito.AsyncEx.Synchronous;
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
                    var producer = link.CreateProducer(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                        var q = await cfg.QueueDeclare(queueName, true, false, true);

                        await cfg.Bind(q, ex);

                        return ex;
                    }, config: cfg => cfg.ConfirmsMode(true));

                    producer.PublishAsync(new byte[] {})
                        .WaitAndUnwrapException();
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                        var q = await cfg.QueueDeclarePassive(queueName);

                        await cfg.ExchangeDelete(ex);
                        await cfg.QueueDelete(q);
                    })
                        .WaitAndUnwrapException();
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
                        var q = await cfg.QueueDeclare(queueName, true, false, true);

                        await cfg.Bind(q, ex);

                        return ex;
                    }, config: cfg => cfg.ConfirmsMode(true)))
                    {
                        producer.PublishAsync(new byte[] {}, publishProperties: new LinkPublishProperties
                        {
                            Mandatory = true
                        })
                            .WaitAndUnwrapException();
                    }
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                        var q = await cfg.QueueDeclarePassive(queueName);

                        await cfg.QueueDelete(q);
                        await cfg.ExchangeDelete(ex);
                    })
                        .WaitAndUnwrapException();
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
                            producer.PublishAsync(new byte[] {}, publishProperties: new LinkPublishProperties
                            {
                                Mandatory = true
                            });
                        });
                    }
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                        await cfg.ExchangeDelete(ex);
                    })
                        .WaitAndUnwrapException();
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
                    var producer = link.CreateProducer(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                        var q = await cfg.QueueDeclare(queueName, false, false, true);

                        await cfg.Bind(q, ex);

                        return ex;
                    }, config: cfg => cfg.ConfirmsMode(false));

                    producer.PublishAsync(new byte[] {})
                        .WaitAndUnwrapException();
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                        var q = await cfg.QueueDeclarePassive(queueName);

                        await cfg.ExchangeDelete(ex);
                        await cfg.QueueDelete(q);
                    })
                        .WaitAndUnwrapException();
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
                        Assert.Throws<TaskCanceledException>(
                            () => { producer.PublishAsync(new byte[] {}, TimeSpan.Zero).WaitAndUnwrapException(); });
                    }
                }
                finally
                {
                    link.ConfigureTopologyAsync(async cfg =>
                    {
                        var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                        await cfg.ExchangeDelete(ex);
                    }).WaitAndUnwrapException();
                }
            }
        }
    }
}