#region Usings

using System.Threading.Tasks;
using RabbitLink.Exceptions;

#endregion

namespace RabbitLink.Messaging.Internals
{
    internal class LinkPulledMessage : LinkConsumedMessage, ILinkPulledMessage
    {
        #region Fields

        private readonly TaskCompletionSource<object> _resultTcs
            = new TaskCompletionSource<object>();

        #endregion

        #region Ctor

        public LinkPulledMessage(
            ILinkConsumedMessage message
        ) : base(
            message.Body,
            message.Properties,
            message.RecieveProperties,
            message.Cancellation
        )
        {
        }

        #endregion

        #region Properties

        public Task ResultTask => _resultTcs.Task;

        #endregion

        internal void Cancel()
        {
            _resultTcs.TrySetCanceled(Cancellation);
        }

        #region ILinkPulledMessage Members

        public void Ack()
        {
            _resultTcs.TrySetResult(null);
        }

        public void Nack(bool requeue = false)
        {
            _resultTcs.TrySetException(new LinkConsumerNackException(
                requeue, $"NACKed by {nameof(ILinkPulledMessage)}"
            ));
        }

        #endregion
    }
}