#region Usings

using System;
using System.Collections.Generic;
using RabbitLink.Serialization;

#endregion

namespace RabbitLink.Configuration
{
    public interface ILinkConfigurationTypeNameMapBuilder
    {
        ILinkConfigurationTypeNameMapBuilder Clear();
        ILinkConfigurationTypeNameMapBuilder Set<T>(string name) where T : class;
        ILinkConfigurationTypeNameMapBuilder Set(Type type, string name);
        ILinkConfigurationTypeNameMapBuilder Set(IDictionary<Type, string> values);

        ILinkConfigurationTypeNameMapBuilder SetParent(ILinkTypeNameMapping parent);
    }
}