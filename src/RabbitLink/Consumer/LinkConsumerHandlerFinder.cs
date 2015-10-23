#region Usings

using System;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkConsumerHandlerFinder
    {
        private readonly Func<ILinkRecievedMessage<byte[]>, ILinkTypeNameMapping, LinkConsumerHandler> _findFunc;


        public LinkConsumerHandlerFinder(
            Func<ILinkRecievedMessage<byte[]>, ILinkTypeNameMapping, LinkConsumerHandler> findFunc)
        {
            if (findFunc == null)
                throw new ArgumentNullException(nameof(findFunc));

            _findFunc = findFunc;
        }

        public LinkConsumerHandler Find(ILinkRecievedMessage<byte[]> message, ILinkTypeNameMapping mapping)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            return _findFunc(message, mapping);
        }
    }
}