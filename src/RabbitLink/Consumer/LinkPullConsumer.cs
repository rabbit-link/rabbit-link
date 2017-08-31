﻿using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Builders;
using RabbitLink.Messaging;

namespace RabbitLink.Consumer
{
    internal class LinkPullConsumer:ILinkPullConsumer
    {
        private readonly ILinkConsumer _consumer;
        private readonly object _sync = new object();
        private bool _disposed;

        public LinkPullConsumer(ILinkConsumerBuilder consumerBuilder, TimeSpan getMessageTimeout)
        {
            if(consumerBuilder == null)
                throw new ArgumentNullException(nameof(consumerBuilder));

            if(getMessageTimeout < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(getMessageTimeout), "Must be greater or equal zero");

            GetMessageTimeout = getMessageTimeout;

            _consumer = consumerBuilder
                .ErrorStrategy(new LinkConsumerDefaultErrorStrategy())
                .Handler(OnMessageRecieved)
                .OnStateChange(OnStateChanged)
                .Build();
        }

        private void OnStateChanged(LinkConsumerState oldState, LinkConsumerState newsState)
        {
            if (newsState == LinkConsumerState.Disposed)
            {
                OnDispose();
            }
        }

        private Task OnMessageRecieved(ILinkConsumedMessage message)
        {
            throw new NotImplementedException();
        }

        private void OnDispose()
        {
            if(_disposed)
                return;

            lock (_sync)
            {
                if(_disposed)
                    return;



                _disposed = true;
            }
        }

        public void Dispose()
            => _consumer.Dispose();

        public Guid Id => _consumer.Id;
        public ushort PrefetchCount => _consumer.PrefetchCount;
        public bool AutoAck => _consumer.AutoAck;
        public int Priority => _consumer.Priority;
        public bool CancelOnHaFailover => _consumer.CancelOnHaFailover;
        public bool Exclusive => _consumer.Exclusive;

        public Task WaitReadyAsync(CancellationToken? cancellation = null)
            => _consumer.WaitReadyAsync(cancellation);
        public TimeSpan GetMessageTimeout { get; }

        public ILinkPulledMessage GetMessage(CancellationToken? cancellation = null)
        {
            throw new NotImplementedException();
        }
    }
}

