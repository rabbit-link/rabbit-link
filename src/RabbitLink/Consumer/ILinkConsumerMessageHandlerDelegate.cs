#region Usings

using System.Threading.Tasks;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    /// <summary>
    ///     Message handler delegate for <see cref="ILinkConsumer" />
    /// </summary>
    /// <returns>Task when handle</returns>
    public delegate Task LinkConsumerMessageHandlerDelegate(
        ILinkConsumedMessage message
    );
}