#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using RabbitLink.Helpers;
using RabbitMQ.Client;

#endregion

namespace RabbitLink.Messaging
{
    public class LinkMessageProperties 
    {
        private readonly Dictionary<string, dynamic> _properties = new Dictionary<string, dynamic>();

        public LinkMessageProperties()
        {
        }

        internal LinkMessageProperties(IBasicProperties from)
        {
            CopyFrom(from);
        }

        public string AppId
        {
            get { return _properties.GetOrDefault(nameof(AppId)); }
            set { _properties[nameof(AppId)] = CheckShortString(nameof(value), value); }
        }

        public bool AppIdPresent => _properties.ContainsKey(nameof(AppId));

        public string ClusterId
        {
            get { return _properties.GetOrDefault(nameof(ClusterId)); }
            set { _properties[nameof(ClusterId)] = value; }
        }

        public bool ClusterIdPresent => _properties.ContainsKey(nameof(ClusterId));

        public string ContentEncoding
        {
            get { return _properties.GetOrDefault(nameof(ContentEncoding)); }
            set { _properties[nameof(ContentEncoding)] = CheckShortString(nameof(value), value); }
        }

        public bool ContentEncodingPresent => _properties.ContainsKey(nameof(ContentEncoding));

        public string ContentType
        {
            get { return _properties.GetOrDefault(nameof(ContentType)); }
            set { _properties[nameof(ContentType)] = CheckShortString(nameof(value), value); }
        }

        public bool ContentTypePresent => _properties.ContainsKey(nameof(ContentType));

        public string CorrelationId
        {
            get { return _properties.GetOrDefault(nameof(CorrelationId)); }
            set { _properties[nameof(CorrelationId)] = CheckShortString(nameof(value), value); }
        }

        public bool CorrelationIdPresent => _properties.ContainsKey(nameof(CorrelationId));

        public LinkMessageDeliveryMode DeliveryMode
        {
            get { return _properties.GetOrDefault(nameof(DeliveryMode)) ?? LinkMessageDeliveryMode.Transient; }
            set { _properties[nameof(DeliveryMode)] = value; }
        }

        public bool DeliveryModePresent => _properties.ContainsKey(nameof(DeliveryMode));

        public string ReplyTo
        {
            get { return _properties.GetOrDefault(nameof(ReplyTo)); }
            set { _properties[nameof(ReplyTo)] = CheckShortString(nameof(value), value); }
        }

        public bool ReplyToPresent => _properties.ContainsKey(nameof(ReplyTo));

        public TimeSpan? Expiration
        {
            get { return _properties.GetOrDefault(nameof(Expiration)); }
            set
            {
                if (value?.TotalMilliseconds < 0 || value?.TotalMilliseconds > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Must be greater or equal 0 and less than Int32.MaxValue");

                _properties[nameof(Expiration)] = value;
            }
        }

        public bool ExpirationPresent => _properties.ContainsKey(nameof(Expiration));

        public string MessageId
        {
            get { return _properties.GetOrDefault(nameof(MessageId)); }
            set { _properties[nameof(MessageId)] = CheckShortString(nameof(value), value); }
        }

        public bool MessageIdPresent => _properties.ContainsKey(nameof(MessageId));

        /// <summary>
        /// Timestamp in UNIX time
        /// </summary>
        public long? TimeStamp
        {
            get { return _properties.GetOrDefault(nameof(TimeStamp)); }
            set { _properties[(nameof(TimeStamp))] = value; }
        }

        public bool TimeStampPresent => _properties.ContainsKey(nameof(TimeStamp));

        public string Type
        {
            get { return _properties.GetOrDefault(nameof(Type)); }
            set { _properties[nameof(Type)] = CheckShortString(nameof(value), value); }
        }

        public bool TypePresent => _properties.ContainsKey(nameof(Type));

        public string UserId
        {
            get { return _properties.GetOrDefault(nameof(UserId)); }
            set { _properties[nameof(UserId)] = CheckShortString(nameof(value), value); }
        }

        public bool UserIdPresent => _properties.ContainsKey(nameof(UserId));

        public byte? Priority
        {
            get { return _properties.GetOrDefault(nameof(Priority)); }
            set { _properties[nameof(Priority)] = value; }
        }

        public bool PriorityPresent => _properties.ContainsKey(nameof(Priority));

        public IDictionary<string, object> Headers { get; } = new Dictionary<string, object>();

        public bool HeadersPresent => Headers?.Any() == true;        

        public void CopyFrom(LinkMessageProperties from)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (from.AppIdPresent) AppId = from.AppId;
            if (from.ClusterIdPresent) ClusterId = from.ClusterId;
            if (from.ContentEncodingPresent) ContentEncoding = from.ContentEncoding;
            if (from.ContentTypePresent) ContentType = from.ContentType;
            if (from.CorrelationIdPresent) CorrelationId = from.CorrelationId;
            if (from.DeliveryModePresent) DeliveryMode = @from.DeliveryMode;
            if (from.ReplyToPresent) ReplyTo = from.ReplyTo;
            if (from.ExpirationPresent) Expiration = from.Expiration;
            if (from.MessageIdPresent) MessageId = from.MessageId;
            if (from.TimeStampPresent) TimeStamp = from.TimeStamp;
            if (from.TypePresent) Type = from.Type;
            if (from.UserIdPresent) UserId = from.UserId;
            if (from.PriorityPresent) Priority = from.Priority;

            if (from.HeadersPresent)
            {
                foreach (var header in from.Headers)
                {
                    Headers[header.Key] = header.Value;
                }
            }
        }

        internal void CopyTo(LinkMessageProperties to)
        {
            if (to == null)
                throw new ArgumentNullException(nameof(to));

            if (AppIdPresent) to.AppId = AppId;
            if (ClusterIdPresent) to.ClusterId = ClusterId;
            if (ContentEncodingPresent) to.ContentEncoding = ContentEncoding;
            if (ContentTypePresent) to.ContentType = ContentType;
            if (CorrelationIdPresent) to.CorrelationId = CorrelationId;
            if (DeliveryModePresent) to.DeliveryMode = DeliveryMode;
            if (ReplyToPresent) to.ReplyTo = ReplyTo;
            if (ExpirationPresent) to.Expiration = Expiration;
            if (MessageIdPresent) to.MessageId = MessageId;
            if (TimeStampPresent) to.TimeStamp = TimeStamp;
            if (TypePresent) to.Type = Type;
            if (UserIdPresent) to.UserId = UserId;
            if (PriorityPresent) to.Priority = Priority;

            if (HeadersPresent)
            {
                foreach (var header in Headers)
                {
                    to.Headers[header.Key] = header.Value;
                }
            }
        }

        public LinkMessageProperties Clone()
        {
            var ret = new LinkMessageProperties();
            CopyTo(ret);
            return ret;
        }

        private static string CheckShortString(string name, string input)
        {
            if (input == null) return null;

            if (input.Length > 255)
            {
                throw new ArgumentOutOfRangeException(name, "Must be less than 256 characters long");
            }

            return input;
        }

        #region BasicPropertiesCopy

        internal void CopyFrom(IBasicProperties from)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (from.IsAppIdPresent()) AppId = from.AppId;
            if (from.IsClusterIdPresent()) ClusterId = from.ClusterId;
            if (from.IsContentEncodingPresent()) ContentEncoding = from.ContentEncoding;
            if (from.IsContentTypePresent()) ContentType = from.ContentType;
            if (from.IsCorrelationIdPresent()) CorrelationId = from.CorrelationId;
            if (from.IsDeliveryModePresent()) DeliveryMode = (LinkMessageDeliveryMode) from.DeliveryMode;
            if (from.IsReplyToPresent()) ReplyTo = from.ReplyTo;
            if (from.IsExpirationPresent()) Expiration = TimeSpan.FromMilliseconds(int.Parse(from.Expiration));
            if (from.IsMessageIdPresent()) MessageId = from.MessageId;
            if (from.IsTimestampPresent()) TimeStamp = from.Timestamp.UnixTime;
            if (from.IsTypePresent()) Type = from.Type;
            if (from.IsUserIdPresent()) UserId = from.UserId;
            if (from.IsPriorityPresent()) Priority = from.Priority;

            if (from.IsHeadersPresent())
            {
                foreach (var header in from.Headers)
                {
                    Headers[header.Key] = header.Value;
                }
            }
        }

        internal void CopyTo(IBasicProperties to)
        {
            if (to == null)
                throw new ArgumentNullException(nameof(to));

            if (AppIdPresent) to.AppId = AppId;
            if (ClusterIdPresent) to.ClusterId = ClusterId;
            if (ContentEncodingPresent) to.ContentEncoding = ContentEncoding;
            if (ContentTypePresent) to.ContentType = ContentType;
            if (CorrelationIdPresent) to.CorrelationId = CorrelationId;
            if (DeliveryModePresent) to.DeliveryMode = (byte) DeliveryMode;
            if (ReplyToPresent) to.ReplyTo = ReplyTo;
            if (ExpirationPresent) to.Expiration = ((int) Expiration.Value.TotalMilliseconds).ToString();
            if (MessageIdPresent) to.MessageId = MessageId;
            if (TimeStampPresent) to.Timestamp = new AmqpTimestamp(TimeStamp.Value);
            if (TypePresent) to.Type = Type;
            if (UserIdPresent) to.UserId = UserId;
            if (PriorityPresent) to.Priority = Priority.Value;

            if (HeadersPresent)
            {
                if (to.Headers == null)
                {
                    to.Headers = new Dictionary<string, object>();
                }

                foreach (var header in Headers)
                {
                    to.Headers[header.Key] = header.Value;
                }
            }
        }

        #endregion
    }
}