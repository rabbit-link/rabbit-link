using System.IO.Compression;
using RabbitLink.Builders;
using RabbitLink.Interceptors;

namespace RabbitLink.Extensions
{
    public static class LinkConsumerBuilderExtensions
    {
        public static ILinkConsumerBuilder WithGzip(this ILinkConsumerBuilder builder, CompressionLevel level = CompressionLevel.Optimal)
        {
            return builder.WithInterception(new GzipMessageInterceptor(level));
        }

        public static ILinkProducerBuilder WithGzip(this ILinkProducerBuilder builder, CompressionLevel level = CompressionLevel.Optimal)
        {
            return builder.WithInterception(new GzipMessageInterceptor(level));
        }
    }
}
