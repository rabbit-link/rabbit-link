using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task<LinkConsumerAckStrategy> Intercept(ILinkConsumedMessage<byte[]> msg, CancellationToken ct, HandleDeliveryDelegate executeCore)
    {
        byte[] decompressedBytes;
        using (var byteStream = new MemoryStream(msg.Body))
        {
            using var newStream = new MemoryStream();
            using var compressor = new GZipStream(byteStream, _level);
            await compressor.CopyToAsync(newStream);
            newStream.Position = 0;
            decompressedBytes = newStream.ToArray();
        }

        var result = new LinkConsumedMessage<byte[]>(decompressedBytes, msg.Properties, msg.ReceiveProperties, msg.Cancellation);
        return await executeCore(result, ct);
    }

    /// <inheritdoc />
    public async Task Intercept(ILinkPublishMessage<byte[]> msg, CancellationToken ct, HandlePublishDelegate executeCore)
    {
        byte[] compressedBytes;
        using (var byteStream = new MemoryStream(msg.Body))
        {
            using var newStream = new MemoryStream();
            using var compressor = new GZipStream(newStream, _level);
            await byteStream.CopyToAsync(compressor);
            newStream.Position = 0;
            compressedBytes = newStream.ToArray();
        }

        var result = new LinkPublishMessage<byte[]>(compressedBytes, msg.Properties, msg.PublishProperties);
        await executeCore(result, ct);
    }
}
