#region Usings

using System;
using System.Threading;

#endregion

namespace RabbitLink.Internals.Queues
{
    internal class ActionQueueItem<TActor> : WorkItem<Func<TActor, object>, object>
    {
        #region Ctor

        public ActionQueueItem(Func<TActor, object> value, CancellationToken cancellationToken) : base(value,
            cancellationToken)
        {
        }

        #endregion
    }
}