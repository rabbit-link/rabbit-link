#region Usings

using System;
using RabbitLink.Messaging;
using Xunit;

#endregion

namespace RabbitLink.UnitTests.Messaging
{
    public class LinkGuidMessageIdGeneratorTests
    {
        [Fact]
        public void SetMessageId()
        {
            var gen = new LinkGuidMessageIdGenerator();

            var body = new byte[1];
            var props = new LinkMessageProperties();
            var pubProps = new LinkPublishProperties();

            gen.SetMessageId(body, props, pubProps);

            Assert.NotNull(props.MessageId);
            Assert.True(Guid.TryParse(props.MessageId, out var _));
        }
    }
}
