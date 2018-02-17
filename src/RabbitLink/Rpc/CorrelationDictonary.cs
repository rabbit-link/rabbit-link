using System.Collections.Concurrent;
using System.Threading.Tasks;
using RabbitLink.Messaging;

namespace RabbitLink.Rpc
{
    internal class CorrelationDictonary
        : ConcurrentDictionary<string, TaskCompletionSource<ILinkConsumedMessage<byte[]>>>
    {

    }
}
