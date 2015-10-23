#region Usings

using System;
using System.Text;
using Newtonsoft.Json;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Serialization
{
    /// <summary>
    ///     Implementation of <see cref="ILinkMessageSerializer" />
    ///     uses JSON.Net
    /// </summary>
    public class JsonMessageSerializer : ILinkMessageSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonMessageSerializer()
        {
            _settings = new JsonSerializerSettings();
        }

        public JsonMessageSerializer(JsonSerializerSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _settings = settings;
        }

        public ILinkMessage<byte[]> Serialize<T>(ILinkMessage<T> message) where T : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var properties = message.Properties.Clone();
            properties.ContentType = "application/json";
            properties.ContentEncoding = Encoding.UTF8.WebName;

            if (message.Body == null)
            {
                return new LinkMessage<byte[]>(new byte[] {}, properties);
            }

            var stringBody = JsonConvert.SerializeObject(message.Body, _settings);

            return new LinkMessage<byte[]>(Encoding.UTF8.GetBytes(stringBody), properties);
        }

        public ILinkMessage<T> Deserialize<T>(ILinkMessage<byte[]> message) where T : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var properties = message.Properties.Clone();

            if (message.Body == null || message.Body.Length == 0)
            {
                return new LinkMessage<T>(null, properties);
            }

            var stringBody = Encoding.UTF8.GetString(message.Body);
            var body = JsonConvert.DeserializeObject<T>(stringBody, _settings);

            return new LinkMessage<T>(body, properties);
        }
    }
}