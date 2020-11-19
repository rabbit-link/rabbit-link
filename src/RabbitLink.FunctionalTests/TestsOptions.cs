#region Usings

using System;
using RabbitLink.Builders;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.FunctionalTests
{
    internal static class TestsOptions
    {
        public static string ConnectionString { get; } = "amqp://localhost";

        public static string TestExchangeName => $"link.test.{Guid.NewGuid():D}.exchange";
        public static string TestQueueName => $"link.test.{Guid.NewGuid():D}.queue";

        public static IConnection GetConnection()
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(ConnectionString),
                AutomaticRecoveryEnabled = false,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            return factory.CreateConnection();
        }

        public static ILinkBuilder GetLinkBuilder()
        {
            return LinkBuilder.Configure.Uri(ConnectionString);
        }
    }
}
