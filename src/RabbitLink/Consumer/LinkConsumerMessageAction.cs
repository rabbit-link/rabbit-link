using System.Threading;
using RabbitLink.Internals.Channels;

namespace RabbitLink.Consumer
{
    internal class LinkConsumerMessageAction : ChannelItem
    {
        #region Ctor

        public LinkConsumerMessageAction(
            ulong seq,
            LinkConsumerAckStrategy strategy,
            CancellationToken cancellation
        ) : base(cancellation)
        {
            Seq = seq;
            Strategy = strategy;
        }

        #endregion

        #region Properties

        public ulong Seq { get; }
        public LinkConsumerAckStrategy Strategy { get; }

        #endregion
    }
}
