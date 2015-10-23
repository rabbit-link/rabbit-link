#region Usings

using System;

#endregion

namespace RabbitLink
{
    public class LinkDisconnectedEventArgs : EventArgs
    {
        public LinkDisconnectedEventArgs(LinkDisconnectedInitiator initiator, uint code, string message)
        {
            Initiator = initiator;
            Code = code;
            Message = message;
        }


        public LinkDisconnectedInitiator Initiator { get; }

        public uint Code { get; }
        public string Message { get; }
    }
}