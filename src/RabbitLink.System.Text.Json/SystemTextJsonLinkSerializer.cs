using System;
using System.IO;
using System.Text.Json;
using RabbitLink.Builders;
using RabbitLink.Messaging;

namespace RabbitLink.Serialization
{
    /// <summary>
    /// Realization <see cref="ILinkSerializer"/> using <see cref="System.Text.Json.JsonSerializer"/>
    /// </summary>
    public class SystemTextJsonLinkSerializer : ILinkSerializer
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Construct new instance of <see cref="SystemTextJsonLinkSerializer"/>
        /// </summary>
        /// <param name="options">Use <see cref="JsonSerializerOptions"/></param>
        public SystemTextJsonLinkSerializer(JsonSerializerOptions options = null)
        {
            _options = options;
        }

        /// <inheritdoc/>
        public byte[] Serialize<TBody>(TBody body, LinkMessageProperties properties) where TBody : class
        {
            properties.ContentType = "application/json; charset=utf-8";
            return JsonSerializer.SerializeToUtf8Bytes(body, _options);
        }

        /// <inheritdoc/>
        public TBody Deserialize<TBody>(byte[] body, LinkMessageProperties properties) where TBody : class
            => JsonSerializer.Deserialize<TBody>(body.AsSpan(), _options);
    }
}
