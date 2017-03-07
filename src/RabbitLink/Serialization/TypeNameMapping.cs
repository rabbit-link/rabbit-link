#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;

#endregion

namespace RabbitLink.Serialization
{
    public interface ILinkTypeNameMapping
    {
        string Map(Type type);
        string Map<T>();
        Type Map(string name);
    }

    internal class LinkTypeNameMapping : ILinkTypeNameMapping
    {
        private readonly IDictionary<string, Type> _nameTypeMap = new Dictionary<string, Type>();
        private readonly IDictionary<Type, string> _typeNameMap = new Dictionary<Type, string>();

        public LinkTypeNameMapping()
        {
        }

        public LinkTypeNameMapping(IDictionary<Type, string> values)
        {
            Set(values);
        }

        public LinkTypeNameMapping(LinkTypeNameMapping mapping)
        {
            Set(mapping);
        }        

        public string Map(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            string name;
            _typeNameMap.TryGetValue(type, out name);

            return name;
        }

        public string Map<T>()
        {
            return Map(typeof (T));
        }

        public Type Map(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            name = name.Trim();

            Type type;
            _nameTypeMap.TryGetValue(name, out type);

            return type;
        }

        public void Set(Type type, string name)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type == typeof (byte[]))
                throw new ArgumentException("Byte[] not supported, it used to handle raw messages");

            if (type == typeof (object))
                throw new ArgumentException("Object not supported, please use concrete type");

            if (!type.GetTypeInfo().IsClass)
                throw new ArgumentException("Type must be a class", nameof(type));


            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            name = name.Trim();

            _nameTypeMap[name] = type;
            _typeNameMap[type] = name;
        }

        public void Set(IDictionary<Type, string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            foreach (var value in values)
            {
                Set(value.Key, value.Value);
            }
        }

        public void Set(LinkTypeNameMapping mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            Set(mapping._typeNameMap);
        }

        public void Remove(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            string name;

            if (_typeNameMap.TryGetValue(type, out name))
            {
                _typeNameMap.Remove(type);
                _nameTypeMap.Remove(name);
            }
        }

        public void Remove(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            name = name.Trim();

            Type type;

            if (_nameTypeMap.TryGetValue(name, out type))
            {
                _nameTypeMap.Remove(name);
                _typeNameMap.Remove(type);
            }
        }

        public void Clear()
        {
            _typeNameMap.Clear();
            _nameTypeMap.Clear();
        }

        public LinkTypeNameMapping Clone()
        {
            var ret = new LinkTypeNameMapping();
            ret.Set(_typeNameMap);
            return ret;
        }
    }
}