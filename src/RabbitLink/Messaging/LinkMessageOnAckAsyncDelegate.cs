#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Messaging
{
    internal delegate Task LinkMessageOnAckAsyncDelegate(Action onSuccess, CancellationToken cancellation);
}