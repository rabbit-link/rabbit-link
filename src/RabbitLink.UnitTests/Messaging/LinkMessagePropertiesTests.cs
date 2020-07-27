#region Usings

using System;
using RabbitLink.Messaging;
using RabbitLink.UnitTests.Helpers;
using Xunit;

#endregion

namespace RabbitLink.UnitTests.Messaging
{
    public class LinkMessagePropertiesTests
    {
        #region Properties

        [Fact]
        public void AppIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.AppId);

            props.AppId = "test";

            Assert.Equal("test", props.AppId);
        }

        [Fact]
        public void ClusterIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.ClusterId);

            props.ClusterId = "test";

            Assert.Equal("test", props.ClusterId);
        }

        [Fact]
        public void ContentEncodingProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.ContentEncoding);

            props.ContentEncoding = "test";

            Assert.Equal("test", props.ContentEncoding);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                props.ContentEncoding = new string('a', 257);
            });
        }

        [Fact]
        public void ContentTypeProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.ContentType);

            props.ContentType = "test";

            Assert.Equal("test", props.ContentType);
        }

        [Fact]
        public void CorrelationIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Null(props.CorrelationId);

            props.CorrelationId = "test";

            Assert.Equal("test", props.CorrelationId);

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

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                props.Expiration = TimeSpan.FromSeconds(-1);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                props.Expiration = TimeSpan.FromSeconds(int.MaxValue);
            });
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

            Assert.Equal("test", props.UserId);
        }

        [Fact]
        public void HeadersProperty()
        {
            var props = new LinkMessageProperties();

            Assert.Empty(props.Headers);

            props.Headers.Add("test", 1);

            Assert.Equal(1, props.Headers["test"]);
            Assert.NotEmpty(props.Headers);
        }

        #endregion
    }
}
