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

        public LinkJsonSerializer()
        {
            _settings = new JsonSerializerSettings();
        }

        public LinkJsonSerializer(JsonSerializerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }


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

        public TBody Deserialize<TBody>(byte[] body, LinkMessageProperties properties) where TBody : class
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            if (body == null)
            {
                return null;
            }

            var stringBody = Encoding.UTF8.GetString(body);
            return JsonConvert.DeserializeObject<TBody>(stringBody, _settings);
        }
    }
}