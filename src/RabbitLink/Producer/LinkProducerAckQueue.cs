#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using RabbitLink.Exceptions;

#endregion

namespace RabbitLink.Producer
{
    internal class LinkProducerAckQueue
    {
        #region Fields

        private readonly Dictionary<string, Item> _correlationItems = new Dictionary<string, Item>();
        private readonly Dictionary<ulong, Item> _seqItems = new Dictionary<ulong, Item>();

        private readonly object _sync = new object();
        private ulong _minSeq;

        #endregion

        public string Add(LinkProducerMessage message, ulong seq)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            lock (_sync)
            {
                if (seq < _minSeq)
                    throw new ArgumentException("Seq less than minimum seq, do you forget to reset queue?",
                        nameof(seq));

                if (_seqItems.ContainsKey(seq))
                    throw new ArgumentException("Queue already contains message with specified seq", nameof(seq));

                var correlationId = Guid.NewGuid().ToString("D");
                while (_correlationItems.ContainsKey(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString("D");
                }

                var item = new Item(message, seq, correlationId);

                _seqItems[item.Seq] = item;
                _correlationItems[item.CorrelationId] = item;

                if (_minSeq > item.Seq)
                {
                    _minSeq = item.Seq;
                }

                return item.CorrelationId;
            }
        }

        public void Return(string correlationId, string reason)
        {
            Item item;

            lock (_sync)
            {
                item = TakeItem(correlationId);
            }

            item?.Message?.TrySetException(new LinkMessageReturnedException(reason));
        }

        public void Ack(ulong seq, bool multiple)
        {
            var items = TakeItems(seq, multiple);

            while (items.Count > 0)
            {
                var item = items.Dequeue();
                item.Message.TrySetResult();
            }
        }

        public void Nack(ulong seq, bool multiple)
        {
            var items = TakeItems(seq, multiple);

            var ex = new LinkMessageNackedException();
            while (items.Count > 0)
            {
                var item = items.Dequeue();
                item.Message.TrySetException(ex);
            }
        }

        public Queue<LinkProducerMessage> Reset()
        {
            var ret = new Queue<LinkProducerMessage>();

            lock (_sync)
            {
                var messages = _seqItems
                    .OrderBy(x => x.Key)
                    .Select(x => x.Value.Message);

                foreach (var message in messages)
                {
                    ret.Enqueue(message);
                }

                _minSeq = 0;
                _seqItems.Clear();
                _correlationItems.Clear();
            }

            return ret;
        }

        private Queue<Item> TakeItems(ulong seq, bool multiple)
        {
            var items = new Queue<Item>();
            lock (_sync)
            {
                if (multiple)
                {
                    for (; _minSeq <= seq; _minSeq++)
                    {
                        var item = TakeItem(_minSeq);
                        if (item != null)
                        {
                            items.Enqueue(item);
                        }
                    }
                }
                else
                {
                    var item = TakeItem(seq);
                    if (item != null)
                    {
                        items.Enqueue(item);
                    }
                }
            }
            return items;
        }

        private Item TakeItem(ulong seq)
        {
            if (_seqItems.TryGetValue(seq, out var item))
            {
                _seqItems.Remove(item.Seq);
                _correlationItems.Remove(item.CorrelationId);

                return item;
            }

            return null;
        }

        private Item TakeItem(string correlationId)
        {
            if (_correlationItems.TryGetValue(correlationId, out var item))
            {
                _seqItems.Remove(item.Seq);
                _correlationItems.Remove(item.CorrelationId);

                return item;
            }

            return null;
        }

        #region Nested types

        #region Item

        private class Item
        {
            #region Ctor

            public Item(LinkProducerMessage message, ulong seq, string correlationId)
            {
                Message = message ?? throw new ArgumentNullException(nameof(message));
                Seq = seq;

                if (string.IsNullOrWhiteSpace(correlationId))
                    throw new ArgumentNullException(nameof(correlationId));

                CorrelationId = correlationId;
            }

            #endregion

            #region Properties

            public LinkProducerMessage Message { get; }
            public ulong Seq { get; }
            public string CorrelationId { get; }

            #endregion
        }

        #endregion

        #endregion
    }
}