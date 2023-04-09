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
    public Task<LinkConsumerAckStrategy> Intercept(ILinkConsumedMessage<byte[]> msg, CancellationToken ct, HandleDeliveryDelegate executeCore)
    {
        byte[] decompressedBytes;
        using (var sourceStream = new MemoryStream(msg.Body))
        {
            using var decompressedStream = new MemoryStream();
            using var compressor = new GZipStream(sourceStream, CompressionMode.Decompress);
            compressor.CopyTo(decompressedStream);
            compressor.Close();
            decompressedBytes = decompressedStream.ToArray();
        }

        var result = new LinkConsumedMessage<byte[]>(decompressedBytes, msg.Properties, msg.ReceiveProperties, msg.Cancellation);
        return executeCore(result, ct);
    }

    /// <inheritdoc />
    /// <remarks> Async overload is not used due to lack of io-dependent logic. </remarks>
    public Task Intercept(ILinkPublishMessage<byte[]> msg, CancellationToken ct, HandlePublishDelegate executeCore)
    {
        byte[] compressedBytes;
        using (var incomingStream = new MemoryStream(msg.Body))
        {
            using var resultStream = new MemoryStream();
            using var compressor = new GZipStream(resultStream, _level);
            incomingStream.CopyTo(compressor);
            compressor.Close();
            compressedBytes = resultStream.ToArray();
        }

        var result = new LinkPublishMessage<byte[]>(compressedBytes, msg.Properties, msg.PublishProperties);
        return executeCore(result, ct);
    }
}
