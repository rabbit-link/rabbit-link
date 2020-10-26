#region Usings

using System;
using RabbitLink.Builders;

#endregion

namespace RabbitLink
{
    /// <summary>
    ///     RabbitMQ connection
    /// </summary>
    public interface ILink : IDisposable
    {
        /// <summary>
        ///     Is Link connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     Gets new producer builder
        /// </summary>
        ILinkProducerBuilder Producer { get; }

        /// <summary>
        ///     Gets new topology builder
        /// </summary>
        ILinkTopologyBuilder Topology { get; }

        /// <summary>
        ///     Gets new consumer builder
        /// </summary>
        ILinkConsumerBuilder Consumer { get; }

        /// <summary>
        ///     Invokes when connected, must not perform blocking operations.
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        ///     Invokes when disconnected, must not perform blocking operations.
        /// </summary>
        event EventHandler Disconnected;

        /// <summary>
        ///     Initializes connection
        /// </summary>
        void Initialize();
    }
}
