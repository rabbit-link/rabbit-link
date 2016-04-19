#region Usings

using System;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkConsumerConfiguration
    {        
        private TimeSpan? _getMessageTimeout;
        private ILinkMessageSerializer _messageSerializer;
        private ushort _prefetchCount;

        public ushort PrefetchCount
        {
            get { return _prefetchCount; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be greater than zero");

                _prefetchCount = value;
            }
        }

        public bool AutoAck { get; set; }

        public int Priority { get; set; }

        public bool CancelOnHaFailover { get; set; }

        public bool Exclusive { get; set; }

        public TimeSpan? GetMessageTimeout
        {
            get { return _getMessageTimeout; }
            set
            {
                if (value != null && value.Value.Ticks < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be positive or zero");

                _getMessageTimeout = value;
            }
        }       

        public LinkTypeNameMapping TypeNameMapping { get; } = new LinkTypeNameMapping();

        public ILinkMessageSerializer MessageSerializer
        {
            get { return _messageSerializer; }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _messageSerializer = value;
            }
        }
    }
}