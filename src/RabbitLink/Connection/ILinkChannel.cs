#region Usings

using System;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Connection
{
    /// <summary>
    ///     Represents automatic recovering <see cref="IModel" />
    /// </summary>
    internal interface ILinkChannel : IDisposable
    {
        #region Properties

        /// <summary>
        ///     Id of channel
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Operating state
        /// </summary>
        LinkChannelState State { get; }

        #endregion

        /// <summary>
        ///     Called when channel disposed
        /// </summary>
        event EventHandler Disposed;

        /// <summary>
        /// Initializes channel
        /// </summary>
        /// <param name="handler">Handler to run callbacks on</param>
        void Initialize(ILinkChannelHandler handler);
    }
}