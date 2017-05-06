#region Usings

using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace RabbitLink.Connection
{
    /// <summary>
    ///     Callbacks handler for <see cref="ILinkChannel" />
    /// </summary>
    internal interface ILinkChannelHandler
    {
        /// <summary>
        ///     Raises when <see cref="ILinkChannel" /> enters Active state
        /// </summary>
        /// <param name="model">Active <see cref="IModel" /></param>
        /// <param name="cancellation">Cancellation to stop processing</param>
        /// <returns></returns>
        Task OnActive(IModel model, CancellationToken cancellation);

        /// <summary>
        ///     Raises when <see cref="ILinkChannel" /> enters Connecting state
        /// </summary>
        /// <param name="cancellation">Cancellation to stop proccessing</param>
        /// <returns></returns>
        Task OnConnecting(CancellationToken cancellation);

        /// <summary>
        ///     Raises when <see cref="ILinkChannel" />'s active <see cref="IModel" /> receives ACK to message
        /// </summary>
        /// <param name="info">ACK information</param>
        void MessageAck(BasicAckEventArgs info);

        /// <summary>
        ///     Raises when <see cref="ILinkChannel" />'s active <see cref="IModel" /> receives NACK to message
        /// </summary>
        /// <param name="info">NACK information</param>
        void MessageNack(BasicNackEventArgs info);

        /// <summary>
        ///     Raises when <see cref="ILinkChannel" />'s active <see cref="IModel" /> receives RETURN to message
        /// </summary>
        /// <param name="info">RETURN information</param>
        void MessageReturn(BasicReturnEventArgs info);
    }
}