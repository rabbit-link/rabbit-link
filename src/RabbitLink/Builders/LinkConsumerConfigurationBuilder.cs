#region Usings

using System;
using RabbitLink.Consumer;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkConsumerBuilder :
        ILinkConsumerBuilder        
    {
        public ILinkConsumerBuilder PrefetchCount(ushort value)
        {
            throw new NotImplementedException();
        }

        public ILinkConsumerBuilder AutoAck(bool value)
        {
            throw new NotImplementedException();
        }

        public ILinkConsumerBuilder Priority(int value)
        {
            throw new NotImplementedException();
        }

        public ILinkConsumerBuilder CancelOnHaFailover(bool value)
        {
            throw new NotImplementedException();
        }

        public ILinkConsumerBuilder Exclusive(bool value)
        {
            throw new NotImplementedException();
        }

        public ILinkConsumerBuilder ErrorStrategy(ILinkConsumerErrorStrategy value)
        {
            throw new NotImplementedException();
        }

        public ILinkConsumerBuilder Handler(LinkConsumerMessageHandlerDelegate value)
        {
            throw new NotImplementedException();
        }

        public ILinkConsumerBuilder GetMessageTimeout(TimeSpan? value)
        {
            throw new NotImplementedException();
        }
    }
}