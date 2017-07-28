#region Usings

using System;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkConsumerConfiguration
    {        
        private TimeSpan? _getMessageTimeout;
        private ushort _prefetchCount;

        public ushort PrefetchCount
        {
            get => _prefetchCount;
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
            get => _getMessageTimeout;
            set
            {
                if (value != null && value.Value.Ticks < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be positive or zero");

                _getMessageTimeout = value;
            }
        }       
    }
}