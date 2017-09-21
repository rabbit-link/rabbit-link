#region Usings

using System;
using System.Diagnostics;
using RabbitLink.Messaging;
using RabbitLink.Tests.Helpers;
using RabbitMQ.Client.Impl;
using Xunit;

#endregion

namespace RabbitLink.Tests.Messaging
{
    public class LinkMessagePropertiesTests
    {
        [Fact]
        public void Ctor()
        {
            new LinkMessageProperties();
        }

        #region Properties

        [Fact]
        public void AppIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.AppId);

            props.AppId = "test";

            Assert.Equal(props.AppId, "test");
        }

        [Fact]
        public void ClusterIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.ClusterId);

            props.ClusterId = "test";

            Assert.Equal(props.ClusterId, "test");
        }

        [Fact]
        public void ContentEncodingProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.ContentEncoding);

            props.ContentEncoding = "test";

            Assert.Equal(props.ContentEncoding, "test");
        }

        [Fact]
        public void ContentTypeProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.ContentType);

            props.ContentType = "test";

            Assert.Equal(props.ContentType, "test");
        }

        [Fact]
        public void CorrelationIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.CorrelationId);

            props.CorrelationId = "test";

            Assert.Equal(props.CorrelationId, "test");

            props.CorrelationId = null;
            Assert.Null(props.CorrelationId);
        }

        [Fact]
        public void DeliveryModeProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Equal(LinkDeliveryMode.Default, props.DeliveryMode);

            props = new LinkMessageProperties
            {
                DeliveryMode = LinkDeliveryMode.Persistent
            };

            Assert.Equal(LinkDeliveryMode.Persistent, props.DeliveryMode);

            props = new LinkMessageProperties
            {
                DeliveryMode = LinkDeliveryMode.Transient
            };

            Assert.Equal(LinkDeliveryMode.Transient, props.DeliveryMode);
        }

        [Fact]
        public void ExpirationProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.Expiration);

            props.Expiration = TimeSpan.FromSeconds(1);

            Assert.Equal(TimeSpan.FromSeconds(1), props.Expiration);
        }

        [Fact]
        public void MessageIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.MessageId);

            props.MessageId = "test";

            Assert.Equal("test", props.MessageId);
        }

        [Fact]
        public void PriorityProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.Priority);

            props.Priority = 5;

            Assert.Equal((byte) 5, props.Priority);
        }

        [Fact]
        public void ReplyToProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.ReplyTo);

            props.ReplyTo = "test";

            Assert.Equal("test", props.ReplyTo);
        }

        [Fact]
        public void TimeStampProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.TimeStamp);

            props.TimeStamp = new DateTime(2015, 01, 01).ToUnixTime();

            Assert.Equal(new DateTime(2015, 01, 01), props.TimeStamp.Value.FromUnixTime());
        }

        [Fact]
        public void TypeProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.Type);

            props.Type = "test";

            Assert.Equal("test", props.Type);
        }

        [Fact]
        public void UserIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.UserId);

            props.UserId = "test";

            Assert.Equal(props.UserId, "test");
        }

        [Fact]
        public void HeadersProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Empty(props.Headers);

            props.Headers.Add("test", 1);

            Assert.Equal(props.Headers["test"], 1);
            Assert.NotEmpty(props.Headers);
        }

        #endregion
    }
}
