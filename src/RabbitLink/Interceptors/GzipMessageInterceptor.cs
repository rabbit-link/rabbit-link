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
public class GzipMessageInterceptor : IMessageInterceptor
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
    public async Task<LinkConsumerAckStrategy> Intercept(ILinkConsumedMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct, HandleDeliveryDelegate executeCore)
    {
        byte[] decompressedBytes;
#if NETSTANDARD2_1
        await using (var byteStream = msg.Body.AsStream())
#else
        using (var byteStream = new MemoryStream(msg.Body.ToArray()))
#endif
        {
            using var newStream = new MemoryStream();
            using var compressor = new GZipStream(byteStream, _level);
#if NETSTANDARD2_1
            await compressor.CopyToAsync(newStream, ct);
#else
            await compressor.CopyToAsync(newStream);
#endif
            newStream.Position = 0;
            decompressedBytes = newStream.ToArray();
        }

        var result = new LinkConsumedMessage<ReadOnlyMemory<byte>>(decompressedBytes, msg.Properties, msg.ReceiveProperties, msg.Cancellation);
        return await executeCore(result, ct);
    }

    /// <inheritdoc />
    public async Task Intercept(ILinkPublishMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct, HandlePublishDelegate executeCore)
    {
        byte[] compressedBytes;

#if NETSTANDARD2_1
        await using (var byteStream = msg.Body.AsStream())
#else
        using (var byteStream = new MemoryStream(msg.Body.ToArray()))
#endif
        {
            using var newStream = new MemoryStream();
            using var compressor = new GZipStream(newStream, _level);
            await byteStream.CopyToAsync(compressor);
            newStream.Position = 0;
            compressedBytes = newStream.ToArray();
        }

        var result = new LinkPublishMessage<ReadOnlyMemory<byte>>(compressedBytes, msg.Properties, msg.PublishProperties);
        await executeCore(result, ct);
    }
}
