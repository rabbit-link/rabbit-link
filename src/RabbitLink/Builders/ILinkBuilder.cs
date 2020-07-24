#region Usings

using System;
using RabbitLink.Connection;
using RabbitLink.Logging;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Builders
{
    /// <summary>
    ///     <see cref="Link" /> configuration builder
    /// </summary>
    public interface ILinkBuilder
    {
        /// <summary>
        /// Name of connection
        /// </summary>
        ILinkBuilder ConnectionName(string value);

        /// <summary>
        /// AMQP connection string
        /// </summary>
        ILinkBuilder Uri(string value);

        /// <summary>
        /// AMQP connection string
        /// </summary>
        ILinkBuilder Uri(Uri value);

        /// <summary>
        ///     Is connection must start automatically
        ///     By default true
        /// </summary>
        ILinkBuilder AutoStart(bool value);

        /// <summary>
        ///     Connection timeout
        ///     By default 10 seconds
        /// </summary>
        ILinkBuilder Timeout(TimeSpan value);

        /// <summary>
        ///     Timeout before next connection attempt
        ///     By default 10 seconds
        /// </summary>
        ILinkBuilder RecoveryInterval(TimeSpan value);

        /// <summary>
        ///     Logger factory
        ///     By default uses <see cref="LinkNullLogger" />
        /// </summary>
        ILinkBuilder LoggerFactory(ILinkLoggerFactory value);

        /// <summary>
        ///     Sets <see cref="LinkMessageProperties.AppId" /> to all published messages, white spaces will be trimmed, must be
        ///     not null or white space
        ///     By default Guid.NewValue().ToString("D")
        /// </summary>
        ILinkBuilder AppId(string value);

        /// <summary>
        /// Sets handler for state changes
        /// </summary>
        ILinkBuilder OnStateChange(LinkStateHandler<LinkConnectionState> handler);

        /// <summary>
        /// Use background threads for connection handling.
        /// By default false
        /// </summary>
        ILinkBuilder UseBackgroundThreadsForConnection(bool value);

        /// <summary>
        /// Serializer for (de)serialize messages.
        /// By default none
        /// </summary>
        ILinkBuilder Serializer(ILinkSerializer value);

        /// <summary>
        /// Builds <see cref="ILink"/> instance
        /// </summary>
        ILink Build();
    }
}
