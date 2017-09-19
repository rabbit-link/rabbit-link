using System;

namespace RabbitLink.Exceptions
{
    /// <summary>
    /// Fires when type name mapping not found
    /// </summary>
    public class LinkTypeNameMappingException:LinkException
    {
        /// <summary>
        /// Constructs instance when no Type header in message
        /// </summary>
        public LinkTypeNameMappingException()
            : base("Message not contains Type header")
        {
        }

        /// <summary>
        /// Constructs instance when Type for Name not found
        /// </summary>
        public LinkTypeNameMappingException(string name)
            : base($"Cannot get mapping for TypeName {name}")
        {
            Name = name;
        }

        /// <summary>
        /// Constructs instance when Name for Type not found
        /// </summary>
        public LinkTypeNameMappingException(Type type)
            : base($"Cannot get mapping for Type {type}")
        {
            Type = type;
        }

        /// <summary>
        /// Mapping type
        /// </summary>
        public Type Type { get; }
        
        /// <summary>
        /// Mapping name
        /// </summary>
        public string Name { get; }

    }
}