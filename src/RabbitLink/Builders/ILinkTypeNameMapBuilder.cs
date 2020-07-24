#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace RabbitLink.Builders
{
    /// <summary>
    ///     Builder for type-name mapping
    /// </summary>
    public interface ILinkTypeNameMapBuilder
    {
        /// <summary>
        ///     Clear map
        /// </summary>
        ILinkTypeNameMapBuilder Clear();

        /// <summary>
        ///     Map type and name
        /// </summary>
        ILinkTypeNameMapBuilder Set<T>(string name) where T : class;

        /// <summary>
        ///     Map type and name
        /// </summary>
        ILinkTypeNameMapBuilder Set(Type type, string name);

        /// <summary>
        ///     Map type and name by merging dictionary with internal state
        /// </summary>
        ILinkTypeNameMapBuilder Set(IDictionary<Type, string> values);
    }
}
