#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    /// Represents message properties
    /// </summary>
    public sealed class LinkMessageProperties
    {
        #region Fields

        private string _appId;
        private string _clusterId;
        private string _contentEncoding;
        private string _contentType;
        private string _correlationId;
        private string _replyTo;
        private TimeSpan? _expiration;
        private string _messageId;
        private string _type;
        private string _userId;

        #endregion

        /// <summary>
        /// Application Id
        /// </summary>
        public string AppId
        {
            get => _appId;
            set => _appId = CheckShortString(nameof(value), value);
        }

        /// <summary>
        /// Cluster Id
        /// </summary>
        public string ClusterId
        {
            get => _clusterId;
            set => _clusterId = NormalizeString(value);
        }

        /// <summary>
        /// Content encoding of body
        /// </summary>
        public string ContentEncoding
        {
            get => _contentEncoding;
            set => _contentEncoding = CheckShortString(nameof(value), value);
        }

        /// <summary>
        /// Content type of body
        /// </summary>
        public string ContentType
        {
            get => _contentType;
            set => _contentType = CheckShortString(nameof(value), value);
        }

        /// <summary>
        /// Request correlation id
        /// </summary>
        public string CorrelationId
        {
            get => _correlationId;
            set => _correlationId = CheckShortString(nameof(value), value);
        }

        /// <summary>
        /// Message delivery mode
        /// </summary>
        public LinkDeliveryMode DeliveryMode { get; set; } = LinkDeliveryMode.Default;

        /// <summary>
        /// Reply to for request
        /// </summary>
        public string ReplyTo
        {
            get => _replyTo;
            set => _replyTo = CheckShortString(nameof(value), value);
        }

        /// <summary>
        /// Message expiration (TTL)
        /// </summary>
        public TimeSpan? Expiration
        {
            get => _expiration;
            set
            {
                if (value?.TotalMilliseconds < 0 || value?.TotalMilliseconds > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Must be greater or equal 0 and less than Int32.MaxValue");

                _expiration = value;
            }
        }

        /// <summary>
        /// Message Id
        /// </summary>
        public string MessageId
        {
            get => _messageId;
            set => _messageId = CheckShortString(nameof(value), value);
        }

        /// <summary>
        /// Timestamp in UNIX time
        /// </summary>
        public long? TimeStamp { get; set; }

        /// <summary>
        /// Message type
        /// </summary>
        public string Type
        {
            get => _type;
            set => _type = CheckShortString(nameof(value), value);
        }

        /// <summary>
        /// Id of sender
        /// </summary>
        public string UserId
        {
            get => _userId;
            set => _userId = CheckShortString(nameof(value), value);
        }

        /// <summary>
        /// Priority of message
        /// </summary>
        public byte? Priority { get; set; }

        /// <summary>
        /// Message headers
        /// </summary>
        public IDictionary<string, object> Headers { get; } = new Dictionary<string, object>();

        #region Private methods

        private static string CheckShortString(string name, string input)
        {
            input = NormalizeString(input);

            if (input != null && input.Length > 255)
            {
                throw new ArgumentOutOfRangeException(name, "Must be less than 256 characters long");
            }

            return input;
        }

        private static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return input.Trim();
        }

        #endregion
    }
}
