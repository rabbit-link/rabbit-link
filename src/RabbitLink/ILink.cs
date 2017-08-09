using System;
using RabbitLink.Builders;

namespace RabbitLink
{
    /// <summary>
    /// RabbitMQ connection
    /// </summary>
    public interface ILink : IDisposable
    {
        /// <summary>
        ///     Is Link connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets new producer builder
        /// </summary>
        ILinkProducerBuilder Producer { get; }

        /// <summary>
        ///     Invokes when connected, must not perform blocking operations.
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        ///     Invokes when disconnected, must not perform blocking operations.
        /// </summary>
        event EventHandler Disconnected;

        /// <summary>
        /// Initializes connection
        /// </summary>
        void Initialize();
    }
}