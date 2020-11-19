using RabbitLink.Builders;
using Microsoft.Extensions.Logging;

namespace RabbitLink.Logging
{
    /// <summary>
    /// Extension methods for <see cref="ILinkBuilder"/>
    /// </summary>
    public static class LinkBuilderExtensions
    {
        /// <summary>
        /// Use <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> as ILinkLogger
        /// </summary>
        /// <param name="builder">Link builder</param>
        /// <param name="factory"><see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> to use</param>
        /// <param name="categoryPrefix">prefix the category of RabbitLink logging messages</param>
        /// <returns></returns>
        public static ILinkBuilder UseMicrosoftExtensionsLogging(this ILinkBuilder builder, ILoggerFactory factory,
            string categoryPrefix = "RabbitLink.")
            => builder.LoggerFactory(new LoggerFactory(factory, categoryPrefix));
    }
}
