#region Usings

using System;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkProducerConfiguration
    {
        private LinkMessageProperties _messageProperties = new LinkMessageProperties();
        private ILinkMessageSerializer _messageSerializer;
        private LinkPublishProperties _publishProperties = new LinkPublishProperties();
        private TimeSpan? _publishTimeout;

        public LinkMessageProperties MessageProperties
        {
            get { return _messageProperties; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _messageProperties = value.Clone();
            }
        }

        public LinkPublishProperties PublishProperties
        {
            get { return _publishProperties; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _publishProperties = value.Clone();
            }
        }

        public bool ConfirmsMode { get; set; }

        public TimeSpan? PublishTimeout
        {
            get { return _publishTimeout; }
            set
            {
                if (value != null && value.Value.Ticks < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be positive or zero");

                _publishTimeout = value;
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

        public bool SetUserId { get; set; }
    }
}