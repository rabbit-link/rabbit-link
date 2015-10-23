#region Usings

using System;
using System.Threading.Tasks;
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
        public void PublishTest()
        {
            var exchangeName = TestsOptions.TestExchangeName;
            var queueName = TestsOptions.TestQueueName;

            var link = new Link(TestsOptions.ConnectionString);

            var producer = link.CreateProducer(async cfg =>
            {
                var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                var q = await cfg.QueueDeclare(queueName, false, false, true);

                await cfg.Bind(q, ex);

                return ex;
            }, config: cfg => cfg.ConfirmsMode(false));

            producer.Publish(new LinkMessage<byte[]>(new byte[] {}));

            link.ConfigureTopology(async cfg =>
            {
                var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                var q = await cfg.QueueDeclarePassive(queueName);

                await cfg.ExchangeDelete(ex);
                await cfg.QueueDelete(q);
            });

            link.Dispose();
        }

        [Fact]
        public void PublishConfirms()
        {
            var exchangeName = TestsOptions.TestExchangeName;
            var queueName = TestsOptions.TestQueueName;

            var link = new Link(TestsOptions.ConnectionString);

            var producer = link.CreateProducer(async cfg =>
            {
                var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                var q = await cfg.QueueDeclare(queueName, true, false, true);

                await cfg.Bind(q, ex);

                return ex;
            }, config: cfg => cfg.ConfirmsMode(true));

            producer.Publish(new LinkMessage<byte[]>(new byte[] {}));

            link.ConfigureTopology(async cfg =>
            {
                var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                var q = await cfg.QueueDeclarePassive(queueName);

                await cfg.ExchangeDelete(ex);
                await cfg.QueueDelete(q);
            });

            link.Dispose();
        }

        [Fact]
        public void PublishConfirmsMandatory()
        {
            var exchangeName = TestsOptions.TestExchangeName;
            var queueName = TestsOptions.TestQueueName;

            var link = new Link(TestsOptions.ConnectionString);

            var producer = link.CreateProducer(async cfg =>
            {
                var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);
                var q = await cfg.QueueDeclare(queueName, true, false, true);

                await cfg.Bind(q, ex);

                return ex;
            }, config: cfg => cfg.ConfirmsMode(true));

            producer.Publish(new LinkMessage<byte[]>(new byte[] {}), new LinkPublishProperties
            {
                Mandatory = true
            });

            link.ConfigureTopology(async cfg =>
            {
                var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                var q = await cfg.QueueDeclarePassive(queueName);

                await cfg.QueueDelete(q);
                await cfg.ExchangeDelete(ex);
            });

            producer.Dispose();

            producer = link.CreateProducer(async cfg =>
            {
                var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);

                return ex;
            }, config: cfg => cfg.ConfirmsMode(true));

            Assert.Throws<LinkMessageReturnedException>(() =>
            {
                producer.Publish(new LinkMessage<byte[]>(new byte[] {}), new LinkPublishProperties
                {
                    Mandatory = true
                });
            });

            link.ConfigureTopology(async cfg =>
            {
                var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                await cfg.ExchangeDelete(ex);
            });

            producer.Dispose();

            link.Dispose();
        }

        [Fact]
        public void PublishTimeoutTest()
        {
            var exchangeName = TestsOptions.TestExchangeName;

            var link = new Link(TestsOptions.ConnectionString);

            var producer = link.CreateProducer(async cfg =>
            {
                var ex = await cfg.ExchangeDeclare(exchangeName, LinkExchangeType.Fanout, autoDelete: true);

                return ex;
            }, config: cfg => cfg.ConfirmsMode(false));

            Assert.Throws<TaskCanceledException>(
                () => { producer.Publish(new LinkMessage<byte[]>(new byte[] {}), TimeSpan.Zero); });

            producer.Dispose();

            try
            {
                link.ConfigureTopology(async cfg =>
                {
                    var ex = await cfg.ExchangeDeclarePassive(exchangeName);
                    await cfg.ExchangeDelete(ex);
                });
            }
            catch
            {
            }

            link.Dispose();
        }
    }
}