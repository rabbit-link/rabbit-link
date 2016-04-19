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

        public byte[] Serialize<T>(T body, LinkMessageProperties properties) where T : class
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

        public T Deserialize<T>(byte[] body, LinkMessageProperties properties) where T : class
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));            

            if (body == null)
            {
                return null;
            }

            var stringBody = Encoding.UTF8.GetString(body);
            return JsonConvert.DeserializeObject<T>(stringBody, _settings);            
        }
    }
}