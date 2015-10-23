#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    public class LinkAckableRecievedMessage<T> : LinkRecievedMessage<T>, ILinkAckableRecievedMessage<T> where T : class
    {
        private readonly Action _ackAction;
        private readonly Action<bool> _nackAction;
        private readonly object _sync = new object();

        public LinkAckableRecievedMessage(
            T body,
            LinkMessageProperties properties,
            LinkRecievedMessageProperties recievedProperties,
            Action ackAction,
            Action<bool> nackAction
            )
            : base(body, properties, recievedProperties)
        {
            if (ackAction == null)
                throw new ArgumentNullException(nameof(ackAction));

            if (nackAction == null)
                throw new ArgumentNullException(nameof(nackAction));

            _ackAction = ackAction;
            _nackAction = nackAction;
        }

        public LinkAckableRecievedMessage(
            ILinkRecievedMessage<T> message,
            Action ackAction,
            Action<bool> nackAction
            )
            : this(message, message.RecievedProperties, ackAction, nackAction)
        {
        }

        public LinkAckableRecievedMessage(
            ILinkMessage<T> message,
            LinkRecievedMessageProperties recievedProperties,
            Action ackAction,
            Action<bool> nackAction
            )
            : this(message.Body, message.Properties, recievedProperties, ackAction, nackAction)
        {
        }

        public bool Acked { get; private set; }
        public bool Nacked { get; private set; }

        public void Ack()
        {
            lock (_sync)
            {
                if (Acked || Nacked)
                    throw new InvalidOperationException($"Already {(Acked ? "Acked" : "Nacked")}");

                _ackAction();
                Acked = true;
            }
        }

        public void Nack(bool requeue = false)
        {
            lock (_sync)
            {
                if (Acked || Nacked)
                    throw new InvalidOperationException($"Already {(Acked ? "Acked" : "Nacked")}");

                _nackAction(requeue);
                Nacked = true;
            }
        }
    }
}