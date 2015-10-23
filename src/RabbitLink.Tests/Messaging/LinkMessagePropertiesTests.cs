#region Usings

using System;
using RabbitLink.Messaging;
using Xunit;

#endregion

namespace RabbitLink.Tests.Messaging
{
    public class LinkMessagePropertiesTests
    {
        [Fact]
        public void Ctor()
        {
            var props = new LinkMessageProperties();
        }

        public void CopyTo()
        {
            var props = new LinkMessageProperties();
        }

        #region Properties

        [Fact]
        public void AppIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.AppIdPresent);
            Assert.Null(props.AppId);

            props.AppId = "test";

            Assert.Equal(props.AppId, "test");
            Assert.True(props.AppIdPresent);
        }

        [Fact]
        public void ClusterIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.ClusterIdPresent);
            Assert.Null(props.ClusterId);

            props.ClusterId = "test";

            Assert.Equal(props.ClusterId, "test");
            Assert.True(props.ClusterIdPresent);
        }

        [Fact]
        public void ContentEncodingProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.ContentEncodingPresent);
            Assert.Null(props.ContentEncoding);

            props.ContentEncoding = "test";

            Assert.Equal(props.ContentEncoding, "test");
            Assert.True(props.ContentEncodingPresent);
        }

        [Fact]
        public void ContentTypeProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.ContentTypePresent);
            Assert.Null(props.ContentType);

            props.ContentType = "test";

            Assert.Equal(props.ContentType, "test");
            Assert.True(props.ContentTypePresent);
        }

        [Fact]
        public void CorrelationIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.CorrelationIdPresent);
            Assert.Null(props.CorrelationId);

            props.CorrelationId = "test";

            Assert.Equal(props.CorrelationId, "test");
            Assert.True(props.CorrelationIdPresent);
        }

        [Fact]
        public void DeliveryModeProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.DeliveryModePresent);
            Assert.Equal(props.DeliveryMode, LinkMessageDeliveryMode.Transient);

            props.DeliveryMode = LinkMessageDeliveryMode.Persistent;

            Assert.Equal(LinkMessageDeliveryMode.Persistent, props.DeliveryMode);
            Assert.True(props.DeliveryModePresent);

            props = new LinkMessageProperties();

            Assert.False(props.DeliveryModePresent);
            Assert.Equal(LinkMessageDeliveryMode.Transient, props.DeliveryMode);

            props.DeliveryMode = LinkMessageDeliveryMode.Transient;

            Assert.Equal(LinkMessageDeliveryMode.Transient, props.DeliveryMode);
            Assert.True(props.DeliveryModePresent);
        }

        [Fact]
        public void ExpirationProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.ExpirationPresent);
            Assert.Null(props.Expiration);

            props.Expiration = TimeSpan.FromSeconds(1);

            Assert.Equal(TimeSpan.FromSeconds(1), props.Expiration);
            Assert.True(props.ExpirationPresent);
        }

        [Fact]
        public void MessageIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.MessageIdPresent);
            Assert.Null(props.MessageId);

            props.MessageId = "test";

            Assert.Equal("test", props.MessageId);
            Assert.True(props.MessageIdPresent);
        }

        [Fact]
        public void PriorityProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.PriorityPresent);
            Assert.Null(props.Priority);

            props.Priority = 5;

            Assert.Equal((byte) 5, props.Priority);
            Assert.True(props.PriorityPresent);
        }

        [Fact]
        public void ReplyToProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.ReplyToPresent);
            Assert.Null(props.ReplyTo);

            props.ReplyTo = "test";

            Assert.Equal("test", props.ReplyTo);
            Assert.True(props.ReplyToPresent);
        }

        [Fact]
        public void TimeStampProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.TimeStampPresent);
            Assert.Null(props.TimeStamp);

            props.TimeStamp = new DateTime(2015, 01, 01);

            Assert.Equal(new DateTime(2015, 01, 01), props.TimeStamp);
            Assert.True(props.TimeStampPresent);
        }

        [Fact]
        public void TypeProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.TypePresent);
            Assert.Null(props.Type);

            props.Type = "test";

            Assert.Equal("test", props.Type);
            Assert.True(props.TypePresent);
        }

        [Fact]
        public void UserIdProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.UserIdPresent);
            Assert.Null(props.UserId);

            props.UserId = "test";

            Assert.Equal(props.UserId, "test");
            Assert.True(props.UserIdPresent);
        }

        [Fact]
        public void HeadersProperty()
        {
            var props = new LinkMessageProperties();

            Assert.False(props.HeadersPresent);
            Assert.Empty(props.Headers);

            props.Headers.Add("test", 1);

            Assert.Equal(props.Headers["test"], 1);
            Assert.True(props.HeadersPresent);
            Assert.NotEmpty(props.Headers);
        }

        #endregion
    }
}