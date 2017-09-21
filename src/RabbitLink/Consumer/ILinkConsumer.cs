#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Consumer
{
    /// <summary>
    ///     Represents RabbitMQ message consumer
    /// </summary>
    public interface ILinkConsumer : IDisposable
    {
        #region Properties

        /// <summary>
        ///     Consumer Id
        /// </summary>
        Guid Id { get; }

        /// <summary>
        ///     Message prefetch count
        /// </summary>
        ushort PrefetchCount { get; }

        /// <summary>
        ///     Auto ack on consume
        /// </summary>
        bool AutoAck { get; }

        /// <summary>
        ///     Consumer priority
        ///     See https://www.rabbitmq.com/consumer-priority.html for more details
        /// </summary>
        int Priority { get; }

        /// <summary>
        ///     Is consumer will be cancelled (then it will be automatically recover) on HA failover
        ///     See https://www.rabbitmq.com/ha.html for more details
        /// </summary>
        bool CancelOnHaFailover { get; }

        /// <summary>
        ///     Is consumer exclusive
        /// </summary>
        bool Exclusive { get; }

        /// <summary>
        /// Serializer
        /// </summary>
        ILinkSerializer Serializer { get; }

        #endregion

        /// <summary>
        ///     Waits for consumer ready
        /// </summary>
        Task WaitReadyAsync(CancellationToken? cancellation = null);
    }
}
