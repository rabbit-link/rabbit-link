using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RabbitLink.Serialization
{
    internal class LinkTypeNameMapping
    {
        private readonly IReadOnlyDictionary<string, Type> _nameMap;
        private readonly IReadOnlyDictionary<Type, string> _typeMap;

        public LinkTypeNameMapping()
        {
            _nameMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            _typeMap = new Dictionary<Type, string>();
        }

        public LinkTypeNameMapping(LinkTypeNameMapping mapping) : this(mapping._typeMap)
        {            
        }

        private LinkTypeNameMapping(IReadOnlyDictionary<Type, string> mapping)
        {
            var nameMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            var typeMap = new Dictionary<Type, string>();

            foreach (var kv in mapping)
            {
                nameMap[kv.Value] = kv.Key;
            }

            foreach (var kv in nameMap)
            {
                typeMap[kv.Value] = kv.Key;
            }

            _nameMap = nameMap;
            _typeMap = typeMap;
        }

        public bool IsEmpty => !_typeMap.Any() || !_nameMap.Any();

        public LinkTypeNameMapping Set(Type type, string name)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (type == typeof(byte[]))
                throw new ArgumentException("Byte[] not supported, it used to handle raw messages");

            if (type == typeof(object))
                throw new ArgumentException("Object not supported, please use concrete type");

            if (!type.GetTypeInfo().IsClass)
                throw new ArgumentException("Type must be a class", nameof(type));

            var mapping = _typeMap.ToDictionary(x => x.Key, x => x.Value);
            mapping[type] = name.Trim();
            
            return new LinkTypeNameMapping(mapping);
        }

        public string Map(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            _typeMap.TryGetValue(type, out var name);
            return name;
        }

        public Type Map(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            name = name.Trim();
            _nameMap.TryGetValue(name, out var type);
            return type;
        }
    }
}