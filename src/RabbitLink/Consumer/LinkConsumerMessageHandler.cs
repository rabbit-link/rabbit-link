#region Usings

using System.Threading.Tasks;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    internal delegate Task LinkConsumerMessageHandler(ILinkPushMessage<byte[]> message);
}