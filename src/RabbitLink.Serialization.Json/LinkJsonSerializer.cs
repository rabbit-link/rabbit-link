using System;
using System.Text;
using Newtonsoft.Json;
using RabbitLink.Messaging;

namespace RabbitLink.Serialization.Json
{
    /// <inheritdoc />
    /// <summary>
    /// JSON Serializer
    /// </summary>
    public class LinkJsonSerializer : ILinkSerializer
    {
        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Constructs instance
        /// </summary>
        public LinkJsonSerializer()
        {
            _settings = new JsonSerializerSettings();
        }

        /// <summary>
        /// Constructs instance
        /// </summary>
        /// <param name="settings">JSON serializer settings</param>
        public LinkJsonSerializer(JsonSerializerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }


        /// <inheritdoc />
        public byte[] Serialize<TBody>(TBody body, LinkMessageProperties properties) where TBody : class
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            properties.ContentType = "application/json";
            properties.ContentEncoding = Encoding.UTF8.WebName;

            if (body == null)
            {
                return null;
            }

            var stringBody = JsonConvert.SerializeObject(body, _settings);

            return Encoding.UTF8.GetBytes(stringBody);
        }

        /// <inheritdoc />
        public TBody Deserialize<TBody>(ReadOnlyMemory<byte> body, LinkMessageProperties properties) where TBody : class
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            if (body.Length <= 0)
            {
                return null;
            }

            ReadOnlySpan<byte> readOnlySpan = body.Span;
#if NETSTANDARD2_1
            var stringBody = System.Text.Encoding.UTF8.GetString(readOnlySpan);

#else
            var stringBody = System.Text.Encoding.UTF8.GetString(readOnlySpan.ToArray());
#endif
            return JsonConvert.DeserializeObject<TBody>(stringBody, _settings);
        }
    }
}
