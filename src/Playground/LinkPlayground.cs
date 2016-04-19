#region Usings

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ColoredConsole;
using Newtonsoft.Json;
using Nito.AsyncEx.Synchronous;
using RabbitLink;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Topology;

#endregion

namespace Playground
{
    internal class LinkPlayground
    {
        public static void Run()
        {
            var loggerFactory = new ActionLinkLoggerFactory(x => new ColoredConsoleLinkLogger(x));

            using (var link = new Link("amqp://localhost", cfg => cfg
                .AutoStart(false)
                .LoggerFactory(loggerFactory)
                ))
            {
                //TestTopology(link);
                Task.Run(async () => await TestPullConsumer(link).ConfigureAwait(false));
                TestPublish(link);


                ColorConsole.WriteLine("Running...");
                Console.ReadLine();
            }
        }

        private static async Task TestPullConsumer(Link link)
        {
            Console.WriteLine("Creating consumer");
            using (var consumer = link.CreateConsumer(
                async cfg =>
                {
                    var exchange = await cfg.ExchangeDeclarePassive("link.consume");
                    var queue = await cfg.QueueDeclare("link.consume");

                    await cfg.Bind(queue, exchange);

                    return queue;
                },
                config:
                    cfg =>
                        cfg.AutoAck(false)
                            .PrefetchCount(1000)
                            .TypeNameMap(map => map.Set<string>("string").Set<MyClass>("woot"))
                ))
            {
                //Console.ReadLine();

                while (true)
                {
                    try
                    {
                        var msg = await consumer.GetMessageAsync();

                        ColorConsole.WriteLine("Message recieved(".Green(), msg.GetType().GenericTypeArguments[0].Name,
                            "):\n".Green(), JsonConvert.SerializeObject(msg, Formatting.Indented));

                        msg.AckAsync();
                    }
                    catch (Exception ex)
                    {
                        ColorConsole.WriteLine("Consume exception:".Red(), ex.ToString());
                    }
                }
            }
        }

        private static void TestPublish(Link link)
        {
            ColorConsole.WriteLine("Starting...");
            link.Initialize();

            using (
                var producer =
                    link.CreateProducer(
                        async cfg => await cfg.ExchangeDeclare("link.consume", LinkExchangeType.Fanout),
                        config: cfg => cfg.TypeNameMap(map => map.Set<string>("string").Set<MyClass>("woot")))
                )
            {
                ColorConsole.WriteLine("Publish");
                var sw = Stopwatch.StartNew();

                var tasks = Enumerable
                    .Range(0, 100)
                    .Select(i => $"Item {i + 1}")
                    .Select(body => producer.PublishAsync(
                        body,
                        new LinkMessageProperties {DeliveryMode = LinkMessageDeliveryMode.Persistent},
                        new LinkPublishProperties {Mandatory = false}
                        ))
                    .ToArray();

                ColorConsole.WriteLine("Waiting for publish end...");
                Task.WaitAll(tasks);
                ColorConsole.WriteLine("Publish done");

                sw.Stop();
                Console.WriteLine("Done in {0:0.###}s", sw.Elapsed.TotalSeconds);
            }
        }

        private static void TestTopology(Link link)
        {
            ColorConsole.WriteLine("Creating topology configurators");

            link.CreatePersistentTopologyConfigurator(PersConfigure, configurationError: PersOnException);

            ColorConsole.WriteLine("Starting...");
            link.Initialize();

            ColorConsole.WriteLine("Configuring topology");
            try
            {
                link.ConfigureTopologyAsync(OnceConfigure, TimeSpan.FromSeconds(10))
                    .WaitAndUnwrapException();
                ColorConsole.WriteLine("Topology configured");
            }
            catch (Exception ex)
            {
                ColorConsole.WriteLine($"Topology config exception: {ex}");
            }
        }

        private static async Task OnceConfigure(ILinkTopologyConfig config)
        {
            var ex = await config.ExchangeDeclare("link.playground.once", LinkExchangeType.Fanout, autoDelete: true);
            var q = await config.QueueDeclareExclusiveByServer();

            await config.Bind(q, ex);
        }

        private static Task PersOnException(Exception exception)
        {
            ColorConsole.WriteLine("PersTopology exception:".Red(), exception.ToString());
            return Task.FromResult((object) null);
        }

        private static async Task PersConfigure(ILinkTopologyConfig config)
        {
            var exchange = await config.QueueDeclareExclusive();
        }

        private class MyClass
        {
            public string Message { get; set; }
        }
    }
}