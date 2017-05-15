#region Usings

using System;
using System.Threading;

#endregion

namespace RabbitLink.Internals.Queues
{
    interface IWorkQueueItem
    {
        #region Properties

        CancellationToken Cancellation { get; }

        #endregion

        bool TrySetException(Exception ex);
        bool TrySetCanceled(CancellationToken cancellation);
    }
}