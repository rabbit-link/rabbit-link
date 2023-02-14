using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;

namespace RabbitLink.Interceptors
{
    /// <summary>
    /// Перехватчик для обертки сообщений в Gzip encoding.
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
        public async Task<ILinkConsumedMessage<byte[]>> Intercept(ILinkConsumedMessage<byte[]> msg, CancellationToken ct)
        {
            using var byteStream = new MemoryStream(msg.Body);

            using var newStream = new MemoryStream();
            using var compressor = new GZipStream(byteStream, _level);
            await compressor.CopyToAsync(newStream);
            newStream.Position = 0;
            return new LinkConsumedMessage<byte[]>(newStream.ToArray(), msg.Properties, msg.ReceiveProperties, msg.Cancellation);
        }

        /// <inheritdoc />
        public async Task<ILinkPublishMessage<byte[]>> Intercept(ILinkPublishMessage<byte[]> msg, CancellationToken cancellation)
        {
            using var byteStream = new MemoryStream(msg.Body);

            using var newStream = new MemoryStream();
            using var compressor = new GZipStream(new MemoryStream(), _level);
            await byteStream.CopyToAsync(compressor);
            newStream.Position = 0;
            return new LinkPublishMessage<byte[]>(newStream.ToArray(), msg.Properties, msg.PublishProperties);
        }
    }

    public interface IMessageInterceptor : IDeliveryInterceptor, IPublishInterceptor
    {
    }

    public interface IPublishInterceptor
    {
        Task<ILinkPublishMessage<byte[]>> Intercept(ILinkPublishMessage<byte[]> msg, CancellationToken cancellation);
    }

    public interface IDeliveryInterceptor
    {
        Task<ILinkConsumedMessage<byte[]>> Intercept(ILinkConsumedMessage<byte[]> msg, CancellationToken ct);
    }
}
