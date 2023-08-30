#region Usings

using System.IO.Compression;
using RabbitLink.Builders;
using RabbitLink.Interceptors;

#endregion

namespace RabbitLink.Extensions;

/// <summary>
/// Extension methods for <see cref="ILinkConsumerBuilder"/> / <see cref="ILinkProducerBuilder"/>.
/// </summary>
public static class BuilderExtensions
{
    /// <summary>
    /// Adds Gzip de-compression to consumer pipeline.
    /// </summary>
    public static ILinkConsumerBuilder WithGzip(this ILinkConsumerBuilder builder)
    {
        return builder.WithInterception(new GzipMessageInterceptor(CompressionLevel.Optimal));
    }

    /// <summary>
    /// Adds Gzip compression to consumer pipeline.
    /// </summary>
    public static ILinkProducerBuilder WithGzip(this ILinkProducerBuilder builder, CompressionLevel level = CompressionLevel.Optimal)
    {
        return builder.WithInterception(new GzipMessageInterceptor(level));
    }
}
