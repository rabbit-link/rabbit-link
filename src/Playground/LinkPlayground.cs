#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitLink;
using RabbitLink.Consumer;
using RabbitLink.Messaging;
using RabbitLink.Serialization.Json;
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

            var link = LinkBuilder.Configure
                .Uri("amqp://localhost/")
                .AutoStart(false)
                .LoggerFactory(new ConsoleLinkLoggerFactory())
                .ConnectionName($"LinkPlayground: {Process.GetCurrentProcess().Id}")
                .Build();

            using (var cts = new CancellationTokenSource())
            using (link)
            {
                var cancellation = cts.Token;
                var ct = Task.Factory.StartNew(() => TestConsumer(link, cancellation), TaskCreationOptions.LongRunning);
//                var ct = Task.Factory
//                    .StartNew(() => TestPullConsumer(link, cancellation), TaskCreationOptions.LongRunning).Unwrap();
                TestPublish(link);

                Console.WriteLine("--- Running ---");
                Console.ReadLine();
                cts.Cancel();
                ct.Wait();
            }
        }

        private static void TestConsumer(ILink link, CancellationToken cancellation)
        {
            var tcs = new TaskCompletionSource<LinkConsumerAckStrategy>();
            tcs.TrySetResult(LinkConsumerAckStrategy.Ack);

            Console.WriteLine("--- Creating consumer ---");
            using (link.Consumer
                .Queue(async cfg =>
                {
                    var exchange = await cfg.ExchangeDeclarePassive("link.consume");
                    var queue = await cfg.QueueDeclare("link.consume");

                    await cfg.Bind(queue, exchange);

                    return queue;
                })
                .AutoAck(false)
                .PrefetchCount(5)
                .Serializer(new LinkJsonSerializer())
                .TypeNameMap(map => map.Set<Msg>("msg").Set<MsgInt>("msg_int").Set<MsgGuid>("msg_guid"))
                .ConsumerTag(id => $"PlaygroundApp:{Environment.MachineName}:{id}")
                .Handler(msg =>
                {
                    Console.WriteLine(
                        "---[ Message ({1}) ]---\n{0}\n---------",
                        JsonConvert.SerializeObject(msg.Body, Formatting.Indented),
                        msg.Body?.GetType()?.Name ?? "None"
                    );

                    return tcs.Task;
                })
                .Build())
            {
                cancellation.WaitHandle.WaitOne();
            }
        }

        private static async Task TestPullConsumer(ILink link, CancellationToken cancellation)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetResult(null);

            Console.WriteLine("--- Creating consumer ---");
            using (var consumer = link.Consumer
                .Queue(async cfg =>
                {
                    var exchange = await cfg.ExchangeDeclarePassive("link.consume");
                    var queue = await cfg.QueueDeclare("link.consume");

                    await cfg.Bind(queue, exchange);

                    return queue;
                })
                .AutoAck(false)
                .PrefetchCount(5)
                .Serializer(new LinkJsonSerializer())
                .TypeNameMap(map => map.Set<Msg>("msg").Set<MsgInt>("msg_int").Set<MsgGuid>("msg_guid"))
                .Pull
                .Build())
            {
                try
                {
                    while (true)
                    {
                        var msg = await consumer.GetMessageAsync<object>(cancellation)
                            .ConfigureAwait(false);

                        Console.WriteLine(
                            "---[ Message ({1}) ]---\n{0}\n---------",
                            JsonConvert.SerializeObject(msg.Body, Formatting.Indented),
                            msg.Body?.GetType()?.Name ?? "None"
                        );

                        msg.Ack();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("--- Consumer Exception: {0}", ex);
                }
            }
        }

        private static void TestPublish(ILink link)
        {
            Console.WriteLine("--- Starting ---");
            link.Initialize();
            Console.WriteLine("--- Started ---");

            using (var producer = link.Producer
                .Exchange(cfg => cfg.ExchangeDeclare("link.consume", LinkExchangeType.Fanout))
                .MessageProperties(new LinkMessageProperties
                {
                    DeliveryMode = LinkDeliveryMode.Persistent
                })
                .PublishProperties(new LinkPublishProperties
                {
                    Mandatory = false
                })
                .PublishTimeout(TimeSpan.FromSeconds(10))
                .Serializer(new LinkJsonSerializer())
                .TypeNameMap(map => map.Set<Msg>("msg").Set<MsgInt>("msg_int").Set<MsgGuid>("msg_guid"))
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
                    .Select(x => new Msg {Message = x})
                    .Select(x => new LinkPublishMessage<Msg>(x));

                for (var i = 0; i < 1000; i++)
                {
                    Task.Run(() => Thread.Sleep(1000));
                }

                var ts = new List<Task>(10);

                foreach (var msg in tasks)
                {
                    ts.Add(producer.PublishAsync(msg));
                }

                Console.WriteLine("--- Waiting for publish end ---");
                Task.WaitAll(ts.ToArray());
                Console.WriteLine("--- Publish done ---");

                sw.Stop();
                Console.WriteLine("--> Done in {0:0.###}s", sw.Elapsed.TotalSeconds);
            }
        }

        private static void TestTopology(ILink link)
        {
            Console.WriteLine("--- Creating topology configurators ---");

            link.Topology
                .Handler(PersConfigure, () => Task.CompletedTask, PersOnException)
                .Build();

            Console.WriteLine("--- Starting ---");
            link.Initialize();

            Console.WriteLine("--- Configuring topology ---");
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    link.Topology
                        .Handler(OnceConfigure)
                        .WaitAsync(cts.Token)
                        .GetAwaiter()
                        .GetResult();
                }

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
            await config.QueueDeclareExclusive();
        }

        public class Msg
        {
            public string Message { get; set; }
        }

        public class MsgInt : Msg
        {
            public int Value { get; set; }
        }

        public class MsgGuid : Msg
        {
            public Guid Value { get; set; }
        }
    }
}
