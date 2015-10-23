#region Usings

using System;

#endregion

namespace RabbitLink.Configuration
{
    public interface ILinkPullConsumerConfigurationBuilder : ILinkConsumerConfigurationBuilder
    {
        /// <summary>
        ///     GetMessageAsync operation timeout
        ///     By default <see cref="ILinkConfigurationBuilder.ConsumerGetMessageTimeout" /> value
        ///     null = infinite
        /// </summary>
        ILinkConsumerConfigurationBuilder GetMessageTimeout(TimeSpan? value);
    }
}