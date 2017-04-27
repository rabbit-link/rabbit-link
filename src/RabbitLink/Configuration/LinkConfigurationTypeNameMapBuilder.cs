#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    internal class LinkConfigurationTypeNameMapBuilder : ILinkConfigurationTypeNameMapBuilder
    {
        public LinkConfigurationTypeNameMapBuilder()
        {
        }

        public LinkConfigurationTypeNameMapBuilder(LinkTypeNameMapping mapping)
        {
            Mapping.Set(mapping);
        }

        internal LinkTypeNameMapping Mapping { get; } = new LinkTypeNameMapping();


        public ILinkConfigurationTypeNameMapBuilder Clear()
        {
            Mapping.Clear();
            return this;
        }

        public ILinkConfigurationTypeNameMapBuilder Set<T>(string name) where T : class
        {
            return Set(typeof (T), name);
        }

        public ILinkConfigurationTypeNameMapBuilder Set(Type type, string name)
        {
            Mapping.Set(type, name);
            return this;
        }

        public ILinkConfigurationTypeNameMapBuilder Set(IDictionary<Type, string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            foreach (var value in values)
            {
                Set(value.Key, value.Value);
            }

            return this;
        }

        public ILinkConfigurationTypeNameMapBuilder SetParent(ILinkTypeNameMapping parent)
        {
            Mapping.SetParent(parent);
            return this;
        }
    }
}