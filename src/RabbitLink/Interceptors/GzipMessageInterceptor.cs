using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD2_1
using CommunityToolkit.HighPerformance;
#endif
using RabbitLink.Consumer;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;

namespace RabbitLink.Interceptors;

/// <summary>
/// Compressing and de-compressing interceptor for publish and consume of rabbit-mq messages.
/// </summary>
public class GzipMessageInterceptor : IDeliveryInterceptor, IPublishInterceptor
{
    private readonly CompressionLevel _level;

    /// <summary>
    /// C-tor.
    /// </summary>
    public GzipMessageInterceptor(CompressionLevel level)
    {
        _level = level;
    }

    /// <inheritdoc />
    /// <remarks> Async overload is not used due to lack of io-dependent logic. </remarks>
    public Task<LinkConsumerAckStrategy> Intercept(ILinkConsumedMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct, HandleDeliveryDelegate executeCore)
    {
        byte[] decompressedBytes;
#if NETSTANDARD2_1
        using (var sourceStream = msg.Body.AsStream())
#else
        using (var sourceStream = new MemoryStream(msg.Body.ToArray()))
#endif
        {
            using var decompressedStream = new MemoryStream();
            using (var compressor = new GZipStream(sourceStream, CompressionMode.Decompress))
            {
                compressor.CopyTo(decompressedStream);
            }

            decompressedBytes = decompressedStream.ToArray();
        }

        var result = new LinkConsumedMessage<ReadOnlyMemory<byte>>(decompressedBytes, msg.Properties, msg.ReceiveProperties, msg.Cancellation);
        return executeCore(result, ct);
    }

    /// <inheritdoc />
    /// <remarks> Async overload is not used due to lack of io-dependent logic. </remarks>
    public Task Intercept(ILinkPublishMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct, HandlePublishDelegate executeCore)
    {
        byte[] compressedBytes;

#if NETSTANDARD2_1
        using (var incomingStream = msg.Body.AsStream())
#else
        using (var incomingStream = new MemoryStream(msg.Body.ToArray()))
#endif
        {
            using var resultStream = new MemoryStream();
            using (var compressor = new GZipStream(resultStream, _level))
            {
                incomingStream.CopyTo(compressor);
            }

            compressedBytes = resultStream.ToArray();
        }

        var result = new LinkPublishMessage<ReadOnlyMemory<byte>>(compressedBytes, msg.Properties, msg.PublishProperties);
        return executeCore(result, ct);
    }
}
