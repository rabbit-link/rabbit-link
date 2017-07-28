#region Usings

using System;
using System.Threading;

#endregion

namespace RabbitLink.Internals.Channels
{
    interface IChannelItem
    {
        #region Properties

        CancellationToken Cancellation { get; }

        #endregion

        bool TrySetException(Exception ex);
        bool TrySetCanceled(CancellationToken cancellation);
    }
}