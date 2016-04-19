using System.Threading;
using System.Threading.Tasks;

namespace RabbitLink.Messaging
{
    internal delegate Task LinkMessageOnNackAsyncDelegate(bool requeue, CancellationToken cancellation);
}