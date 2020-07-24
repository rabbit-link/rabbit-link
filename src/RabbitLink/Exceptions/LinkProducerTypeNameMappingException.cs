#region Usings

using System;

#endregion

namespace RabbitLink.Exceptions
{
    /// <summary>
    ///     Fires when type name mapping not found
    /// </summary>
    public class LinkProducerTypeNameMappingException : LinkException
    {
        /// <summary>
        ///     Constructs instance when Name for Type not found
        /// </summary>
        public LinkProducerTypeNameMappingException(Type type)
            : base($"Cannot get mapping for Type {type}")
        {
            Type = type;
        }

        /// <summary>
        ///     Mapping type
        /// </summary>
        public Type Type { get; }
    }
}
