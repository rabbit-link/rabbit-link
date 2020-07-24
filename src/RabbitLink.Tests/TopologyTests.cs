#region Usings

using System;
using System.Linq;
using RabbitLink.Topology;
using Xunit;

#endregion

namespace RabbitLink.Tests
{
    public class TopologyTests
    {
        [Fact]
        public void TopologyExchangeOperations()
        {
            var exchangeTypes = Enum
                .GetValues(typeof(LinkExchangeType))
                .Cast<LinkExchangeType>()
                .ToArray();

            foreach (var type in exchangeTypes)
            {
                TopologyExchangeConfig(type, false, false, null, false);
            }

            foreach (var type in exchangeTypes)
            {
                TopologyExchangeConfig(type, true, false, null, false);
            }

            foreach (var type in exchangeTypes)
            {
                TopologyExchangeConfig(type, false, true, null, false);
            }

            foreach (var type in exchangeTypes)
            {
                TopologyExchangeConfig(type, false, false, "test", false);
            }
        }

        private void TopologyExchangeConfig(
            LinkExchangeType exchangeType,
            bool durable, bool autoDelete,
            string alternateExchange,
            bool delayed
            )
        {
            using (var rabbitConnection = TestsOptions.GetConnection())
            {
                var rabbitModel = rabbitConnection.CreateModel();

                var exchangeName = TestsOptions.TestExchangeName;

                using (var link = TestsOptions.GetLinkBuilder().Build())
                {
                    Assert.ThrowsAny<Exception>(() => { rabbitModel.ExchangeDeclarePassive(exchangeName); });

                    rabbitModel = rabbitConnection.CreateModel();

                    link.Topology
                        .Handler(async cfg =>
                        {
                            var e1 =
                                await
                                    cfg.ExchangeDeclare(exchangeName, exchangeType, durable, autoDelete,
                                        alternateExchange,
                                        delayed);
                            var e2 =
                                await
                                    cfg.ExchangeDeclare(exchangeName + "-second", exchangeType, durable, autoDelete,
                                        alternateExchange, delayed);

                            await cfg.ExchangeDeclarePassive(exchangeName);

                            await cfg.ExchangeDeclareDefault();

                            await cfg.Bind(e2, e1);
                            await cfg.Bind(e2, e1, "test");
                        })
                        .WaitAsync()
                        .GetAwaiter()
                        .GetResult();

                    rabbitModel.ExchangeDeclarePassive(exchangeName);
                    rabbitModel.ExchangeDeclarePassive(exchangeName + "-second");

                    link.Topology
                        .Handler(async cfg =>
                        {
                            var e1 = await cfg.ExchangeDeclarePassive(exchangeName);
                            var e2 = await cfg.ExchangeDeclarePassive(exchangeName + "-second");

                            await cfg.ExchangeDelete(e1);
                            await cfg.ExchangeDelete(e2);
                        })
                        .WaitAsync()
                        .GetAwaiter()
                        .GetResult();

                    Assert.ThrowsAny<Exception>(() => { rabbitModel.ExchangeDeclarePassive(exchangeName); });
                }
            }
        }
    }
}
