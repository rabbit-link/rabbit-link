#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Messaging
{
    internal delegate Task LinkMessageOnNackAsyncDelegate(
        bool requeue, Action onSuccess, CancellationToken cancellation);
}