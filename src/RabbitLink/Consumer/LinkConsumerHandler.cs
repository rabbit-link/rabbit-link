#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkConsumerHandler
    {
        private readonly Func<ILinkRecievedMessage<byte[]>, ILinkMessageSerializer, Task> _handler;

        public LinkConsumerHandler(Func<ILinkRecievedMessage<byte[]>, ILinkMessageSerializer, Task> handler,
            bool parallel)
        {
            _handler = handler;
            Parallel = parallel;
        }

        public bool Parallel { get; }

        public Task Handle(ILinkRecievedMessage<byte[]> msg, ILinkMessageSerializer serializer)
        {
            return Task.Run(async () => await _handler(msg, serializer).ConfigureAwait(false));
        }
    }
}