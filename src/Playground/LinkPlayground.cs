#region Usings

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitLink;
using RabbitLink.Messaging;
using RabbitLink.Topology;

#endregion

namespace Playground
{
    internal class LinkPlayground
    {
        public static void Run()
        {
            Console.WriteLine("--- Ready to run press enter ---");
            Console.ReadLine();
            
            

            using (var link = LinkBuilder.Configure
                .Uri("amqp://localhost/")
                .AutoStart(false)
                .LoggerFactory(new ConsoleLinkLoggerFactory())
                .Build()
            )
            {
                // ReSharper disable once AccessToDisposedClosure
                //Task.Factory.StartNew(() => TestPullConsumer(link));                
                TestPublish(link);


                Console.WriteLine("--- Running ---");
                Console.ReadLine();
            }
        }

        private static async Task TestPullConsumer(ILink link)
        {
            await Task.Delay(0)
                .ConfigureAwait(false);

            Console.WriteLine("--- Creating consumer ---");
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

                ILinkMessage<object> msg;
                ILinkMessage<object> oldMsg = null;

                while (true)
                {
                    try
                    {
                        msg = await consumer.GetMessageAsync();

                        if (msg == oldMsg)
                        {
                            Console.WriteLine("--- DUPE MESSAGE ---");
                        }

                        oldMsg = msg;
                        
                        Console.WriteLine(
                            "Message recieved ( {0} ):\n{1}", 
                            msg.GetType().GenericTypeArguments[0].Name,
                            JsonConvert.SerializeObject(msg, Formatting.Indented)
                        );
                        
                        try
                        {
                            await msg.AckAsync()
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("--> Consume ACK exception: {0}", ex.ToString());
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("--> Consume exception: {0}", ex.ToString());
                    }
                }
            }
        }

        private static void TestPublish(ILink link)
        {
            Console.WriteLine("--- Starting ---");
            link.Initialize();
            Console.WriteLine("--- Started ---");

            using (var producer = link.Producer
                .Queue(async cfg => await cfg.ExchangeDeclare("link.consume", LinkExchangeType.Fanout))
                .Build()
            )
            {
                Console.WriteLine("--- Producer started, press [ENTER] ---");
                Console.ReadLine();

                Console.WriteLine("--- Publish ---");
                var sw = Stopwatch.StartNew();

                var tasks = Enumerable
                    .Range(0, 10)
                    .Select(i => $"Item {i + 1}")
                    .Select(x => Encoding.UTF8.GetBytes(x));

                foreach (var body in tasks)
                {
                    producer.PublishAsync(
                            body,
                            new LinkMessageProperties {DeliveryMode = LinkDeliveryMode.Persistent},
                            new LinkPublishProperties {Mandatory = false}
                        )
                        .GetAwaiter().GetResult();
                }

                Console.WriteLine("--- Waiting for publish end ---");
                //Task.WaitAll(tasks);
                Console.WriteLine("--- Publish done ---");

                sw.Stop();
                Console.WriteLine("--> Done in {0:0.###}s", sw.Elapsed.TotalSeconds);
            }
        }

        private static void TestTopology(Link link)
        {
            Console.WriteLine("--- Creating topology configurators ---");

            link.CreatePersistentTopologyConfigurator(PersConfigure, configurationError: PersOnException);

            Console.WriteLine("--- Starting ---");
            link.Initialize();

            Console.WriteLine("--- Configuring topology ---");
            try
            {
                link.ConfigureTopologyAsync(OnceConfigure, TimeSpan.FromSeconds(10))
                    .GetAwaiter().GetResult();
                Console.WriteLine("--- Topology configured ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Topology config exception: {ex}");
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
            Console.WriteLine("--> PersTopology exception: {0}", exception.ToString());
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