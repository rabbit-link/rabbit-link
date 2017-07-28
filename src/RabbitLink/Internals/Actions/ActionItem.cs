#region Usings

using System;
using System.Threading;
using RabbitLink.Internals.Channels;

#endregion

namespace RabbitLink.Internals.Actions
{
    internal class ActionItem<TActor> : ChannelItem<Func<TActor, object>, object>
    {
        #region Ctor

        public ActionItem(Func<TActor, object> value, CancellationToken cancellationToken) : base(value,
            cancellationToken)
        {
        }

        #endregion
    }
}