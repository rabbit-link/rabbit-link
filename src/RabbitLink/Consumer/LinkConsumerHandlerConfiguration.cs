#region Usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitLink.Exceptions;
using RabbitLink.Messaging;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkConsumerHandlerConfiguration : ILinkConsumerHandlerConfiguration
    {
        #region Privat methods

        private static LinkConsumerHandler GetDeserializingHandler(Type type,
            Func<ILinkRecievedMessage<object>, Task> onMessage,
            bool parallel)
        {
            return new LinkConsumerHandler((message, serializer) =>
            {
                ILinkMessage<object> msg;

                try
                {
                    msg = serializer.Deserialize(type, message);
                }
                catch (Exception ex)
                {
                    throw new LinkDeserializationException(message, ex);
                }

                var concreteMessage = LinkGenericMessageFactory.ConstructLinkRecievedMessage(
                    type,
                    msg,
                    message.RecievedProperties
                    );

                return onMessage(concreteMessage);
            }, parallel);
        }

        #endregion

        #region Build

        internal LinkConsumerHandlerFinder Build()
        {
            var defaultHandler = _defaultHandler;
            var mappedHandlers = _mappedHandlers;

            return new LinkConsumerHandlerFinder((message, mapping) =>
            {
                var handler = defaultHandler;
                var typeName = message.Properties.Type?.Trim();

                if (!string.IsNullOrEmpty(typeName))
                {
                    var type = mapping.Map(typeName);

                    if (type != null)
                    {
                        MappingHandler mappingHandler;

                        if (mappedHandlers.TryGetValue(type, out mappingHandler))
                            return GetDeserializingHandler(type, mappingHandler.OnMessage, mappingHandler.Parallel);

                        if (mappedHandlers.TryGetValue(typeof (object), out mappingHandler))
                            return GetDeserializingHandler(type, mappingHandler.OnMessage, mappingHandler.Parallel);
                    }
                }

                return handler;
            });
        }

        #endregion

        #region Private classes

        private class MappingHandler
        {
            public MappingHandler(bool parallel, Func<ILinkRecievedMessage<object>, Task> onMessage)
            {
                Parallel = parallel;
                OnMessage = onMessage;
            }

            public Func<ILinkRecievedMessage<object>, Task> OnMessage { get; }
            public bool Parallel { get; }
        }

        #endregion

        #region Fields

        private readonly IDictionary<Type, MappingHandler> _mappedHandlers =
            new Dictionary<Type, MappingHandler>();

        private LinkConsumerHandler _defaultHandler;

        #endregion

        #region Interface implementation

        public ILinkConsumerHandlerConfiguration MappedParallelAsync<T>(Func<ILinkRecievedMessage<T>, Task> onMessage)
            where T : class
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            if (typeof (T) == typeof (byte[]))
                throw new ArgumentException("Byte[] not supported, it used to handle raw messages");

            if (_mappedHandlers.ContainsKey(typeof (T)))
                throw new ArgumentException($"Type {typeof (T)} already registered", nameof(T));

            var handler = new MappingHandler(true, message => onMessage((ILinkRecievedMessage<T>) message));
            _mappedHandlers.Add(typeof (T), handler);

            return this;
        }

        public ILinkConsumerHandlerConfiguration MappedAsync<T>(Func<ILinkRecievedMessage<T>, Task> onMessage)
            where T : class
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            if (typeof (T) == typeof (byte[]))
                throw new ArgumentException("Byte[] not supported, it used to handle raw messages");

            if (_mappedHandlers.ContainsKey(typeof (T)))
                throw new ArgumentException($"Type {typeof (T)} already registered", nameof(T));

            var handler = new MappingHandler(false, message => onMessage((ILinkRecievedMessage<T>) message));
            _mappedHandlers.Add(typeof (T), handler);

            return this;
        }

        public void AllParallelAsync<T>(Func<ILinkRecievedMessage<T>, Task> onMessage) where T : class
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            if (typeof (T) == typeof (byte[]))
            {
                AllParallelAsync(message => onMessage((ILinkRecievedMessage<T>) message));
                return;
            }

            if (typeof (T) == typeof (object))
            {
                throw new ArgumentException("Object not supported", nameof(T));
            }

            _defaultHandler = GetDeserializingHandler(typeof (T),
                message => onMessage((ILinkRecievedMessage<T>) message), true);
        }

        public void AllParallelAsync(Func<ILinkRecievedMessage<byte[]>, Task> onMessage)
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            _defaultHandler = new LinkConsumerHandler((message, serializer) => onMessage(message), true);
        }

        public void AllAsync<T>(Func<ILinkRecievedMessage<T>, Task> onMessage) where T : class
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            if (typeof (T) == typeof (byte[]))
            {
                AllAsync(message => onMessage((ILinkRecievedMessage<T>) message));
                return;
            }

            if (typeof (T) == typeof (object))
            {
                throw new ArgumentException("Object not supported", nameof(T));
            }

            _defaultHandler = GetDeserializingHandler(typeof (T),
                message => onMessage((ILinkRecievedMessage<T>) message), false);
        }

        public void AllAsync(Func<ILinkRecievedMessage<byte[]>, Task> onMessage)
        {
            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            _defaultHandler = new LinkConsumerHandler((message, serializer) => onMessage(message), false);
        }

        #endregion
    }
}