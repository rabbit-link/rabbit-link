#region Usings

using RabbitLink.Consumer;

#endregion

namespace RabbitLink.Configuration
{
    public interface ILinkPushConsumerConfigurationBuilder : ILinkConsumerConfigurationBuilder
    {
        /// <summary>
        ///     Consumer error strategy
        ///     By default <see cref="ILinkConfigurationBuilder.ConsumerErrorStrategy" /> value
        /// </summary>
        /// <remarks>
        ///     Error strategy not working if <see cref="ILinkConsumerConfigurationBuilder.AutoAck" /> is set
        /// </remarks>
        ILinkConsumerConfigurationBuilder ErrorStrategy(ILinkConsumerErrorStrategy value);
    }
}