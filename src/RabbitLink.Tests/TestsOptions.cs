#region Usings

using System;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Tests
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
                Uri = ConnectionString,
                AutomaticRecoveryEnabled = false,
                RequestedConnectionTimeout = (int) TimeSpan.FromSeconds(10).TotalMilliseconds
            };

            return factory.CreateConnection();
        }
    }
}