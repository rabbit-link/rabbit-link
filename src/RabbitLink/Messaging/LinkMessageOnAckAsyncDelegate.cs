using System.Threading;
using System.Threading.Tasks;

namespace RabbitLink.Messaging
{
    internal delegate Task LinkMessageOnAckAsyncDelegate(CancellationToken cancellation);
}