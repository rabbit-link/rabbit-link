using System;
using System.Collections.Generic;
using RabbitLink.Serialization;

namespace RabbitLink.Builders
{
    internal class LinkTypeNameMapBuilder : ILinkTypeNameMapBuilder
    {
        private LinkTypeNameMapping _map = new LinkTypeNameMapping();

        public LinkTypeNameMapBuilder(LinkTypeNameMapping mapping = null)
        {
            if (mapping != null)
            {
                _map = mapping;
            }
        }

        public ILinkTypeNameMapBuilder Clear()
        {
            _map = new LinkTypeNameMapping();
            return this;
        }

        public ILinkTypeNameMapBuilder Set(Type type, string name)
        {
            _map = _map.Set(type, name);
            return this;
        }

        public ILinkTypeNameMapBuilder Set<T>(string name) where T : class
            => Set(typeof(T), name);

        public ILinkTypeNameMapBuilder Set(IDictionary<Type, string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var ret = (ILinkTypeNameMapBuilder) this;
            foreach (var value in values)
            {
                ret = Set(value.Key, value.Value);
            }

            return ret;
        }

        public LinkTypeNameMapping Build()
        {
            return new LinkTypeNameMapping(_map);
        }
    }
}
