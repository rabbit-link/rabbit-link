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
        public void TologyExchangeOperations()
        {
            var exchangeTypes = Enum
                .GetValues(typeof (LinkExchangeType))
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

        public void TopologyExchangeConfig(LinkExchangeType exchangeType, bool durable, bool autoDelete,
            string alternateExhange, bool delayed)
        {
            using (var rabbitConneciton = TestsOptions.GetConnection())
            {
                var rabbitModel = rabbitConneciton.CreateModel();

                var exchangeName = TestsOptions.TestExchangeName;

                using (var link = new Link(TestsOptions.ConnectionString))
                {
                    Assert.ThrowsAny<Exception>(() => { rabbitModel.ExchangeDeclarePassive(exchangeName); });

                    rabbitModel = rabbitConneciton.CreateModel();

                    link.ConfigureTopology(async cfg =>
                    {
                        var e1 =
                            await
                                cfg.ExchangeDeclare(exchangeName, exchangeType, durable, autoDelete, alternateExhange,
                                    delayed);
                        var e2 =
                            await
                                cfg.ExchangeDeclare(exchangeName + "-second", exchangeType, durable, autoDelete,
                                    alternateExhange, delayed);

                        await cfg.ExchangeDeclarePassive(exchangeName);

                        await cfg.ExchangeDeclareDefault();

                        await cfg.Bind(e2, e1);
                        await cfg.Bind(e2, e1, "test");
                    });

                    rabbitModel.ExchangeDeclarePassive(exchangeName);
                    rabbitModel.ExchangeDeclarePassive(exchangeName + "-second");

                    link.ConfigureTopology(async cfg =>
                    {
                        var e1 = await cfg.ExchangeDeclarePassive(exchangeName);
                        var e2 = await cfg.ExchangeDeclarePassive(exchangeName + "-second");

                        await cfg.ExchangeDelete(e1);
                        await cfg.ExchangeDelete(e2);
                    });

                    Assert.ThrowsAny<Exception>(() => { rabbitModel.ExchangeDeclarePassive(exchangeName); });
                }
            }
        }
    }
}