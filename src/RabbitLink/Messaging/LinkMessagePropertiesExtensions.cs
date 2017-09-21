using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace RabbitLink.Messaging
{
    /// <summary>
    /// Extension methods for <see cref="LinkMessageProperties"/>
    /// </summary>
    public static class LinkMessagePropertiesExtensions
    {
        /// <summary>
        /// Extend instance with other instances, returns instance.
        /// </summary>
        public static LinkMessageProperties Extend(this LinkMessageProperties @this,
            params LinkMessageProperties[] others)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (others?.Any() == true)
            {
                foreach (var other in others)
                {
                    if (other.AppId != null) @this.AppId = other.AppId;
                    if (other.ClusterId != null) @this.ClusterId = other.ClusterId;
                    if (other.ContentEncoding != null) @this.ContentEncoding = other.ContentEncoding;
                    if (other.ContentType != null) @this.ContentType = other.ContentType;
                    if (other.CorrelationId != null) @this.CorrelationId = other.CorrelationId;
                    if (other.DeliveryMode != LinkDeliveryMode.Default) @this.DeliveryMode = other.DeliveryMode;
                    if (other.ReplyTo != null) @this.ReplyTo = other.ReplyTo;
                    if (other.Expiration != null) @this.Expiration = other.Expiration;
                    if (other.MessageId != null) @this.MessageId = other.MessageId;
                    if (other.TimeStamp != null) @this.TimeStamp = other.TimeStamp;
                    if (other.Type != null) @this.Type = other.Type;
                    if (other.UserId != null) @this.UserId = other.UserId;
                    if (other.Priority != null) @this.Priority = other.Priority;

                    if (other.Headers.Any())
                    {
                        foreach (var header in other.Headers)
                        {
                            if (header.Value != null)
                            {
                                @this.Headers[header.Key] = header.Value;
                            }
                        }
                    }
                }
            }

            return @this;
        }

        /// <summary>
        /// Makes clone of instance
        /// </summary>
        public static LinkMessageProperties Clone(this LinkMessageProperties @this)
            => new LinkMessageProperties().Extend(@this);

        /// <summary>
        /// Extends instance with <see cref="IBasicProperties"/> instances, return instance
        /// </summary>
        internal static LinkMessageProperties Extend(this LinkMessageProperties @this, params IBasicProperties[] others)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (others?.Any() == true)
            {
                foreach (var other in others)
                {
                    if (other.IsAppIdPresent()) @this.AppId = other.AppId;
                    if (other.IsClusterIdPresent()) @this.ClusterId = other.ClusterId;
                    if (other.IsContentEncodingPresent()) @this.ContentEncoding = other.ContentEncoding;
                    if (other.IsContentTypePresent()) @this.ContentType = other.ContentType;
                    if (other.IsCorrelationIdPresent()) @this.CorrelationId = other.CorrelationId;
                    if (other.IsDeliveryModePresent()) @this.DeliveryMode = (LinkDeliveryMode) other.DeliveryMode;
                    if (other.IsReplyToPresent()) @this.ReplyTo = other.ReplyTo;
                    if (other.IsExpirationPresent())
                        @this.Expiration = TimeSpan.FromMilliseconds(int.Parse(other.Expiration));
                    if (other.IsMessageIdPresent()) @this.MessageId = other.MessageId;
                    if (other.IsTimestampPresent()) @this.TimeStamp = other.Timestamp.UnixTime;
                    if (other.IsTypePresent()) @this.Type = other.Type;
                    if (other.IsUserIdPresent()) @this.UserId = other.UserId;
                    if (other.IsPriorityPresent()) @this.Priority = other.Priority;

                    if (other.IsHeadersPresent() && other.Headers?.Any() == true)
                    {
                        foreach (var header in other.Headers)
                        {
                            if (header.Value != null)
                            {
                                @this.Headers[header.Key] = header.Value;
                            }
                        }
                    }
                }
            }

            return @this;
        }

        /// <summary>
        /// Extend instance with <see cref="LinkMessageProperties"/>. Returns instance.
        /// </summary>
        internal static IBasicProperties Extend(this IBasicProperties @this, params LinkMessageProperties[] others)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (others?.Any() == true)
            {
                foreach (var other in others)
                {
                    if (other.AppId != null) @this.AppId = other.AppId;
                    if (other.ClusterId != null) @this.ClusterId = other.ClusterId;
                    if (other.ContentEncoding != null) @this.ContentEncoding = other.ContentEncoding;
                    if (other.ContentType != null) @this.ContentType = other.ContentType;
                    if (other.CorrelationId != null) @this.CorrelationId = other.CorrelationId;
                    if (other.DeliveryMode != LinkDeliveryMode.Default) @this.DeliveryMode = (byte) other.DeliveryMode;
                    if (other.ReplyTo != null) @this.ReplyTo = other.ReplyTo;
                    if (other.Expiration != null)
                        @this.Expiration = ((int) other.Expiration.Value.TotalMilliseconds).ToString();
                    if (other.MessageId != null) @this.MessageId = other.MessageId;
                    if (other.TimeStamp != null) @this.Timestamp = new AmqpTimestamp(other.TimeStamp.Value);
                    if (other.Type != null) @this.Type = other.Type;
                    if (other.UserId != null) @this.UserId = other.UserId;
                    if (other.Priority != null) @this.Priority = other.Priority.Value;

                    if (other.Headers.Any())
                    {
                        if (@this.Headers == null)
                        {
                            @this.Headers = new Dictionary<string, object>();
                        }

                        foreach (var header in other.Headers)
                        {
                            if (header.Value != null)
                            {
                                @this.Headers[header.Key] = header.Value;
                            }
                        }
                    }
                }
            }

            return @this;
        }
    }
}
