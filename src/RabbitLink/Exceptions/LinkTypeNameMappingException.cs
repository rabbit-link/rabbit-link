#region Usings

using System;

#endregion

namespace RabbitLink.Exceptions
{
    public class LinkTypeNameMappingException : LinkException
    {
        public LinkTypeNameMappingException()
            : base("Message not contains Type header")
        {
        }

        public LinkTypeNameMappingException(string typeName)
            : base($"Cannot get mapping for  TypeName {typeName}")
        {
            TypeName = typeName;
        }

        public LinkTypeNameMappingException(Type type)
            : base($"Cannot get mapping for  Type {type}")
        {
            Type = type;
        }

        public Type Type { get; }
        public string TypeName { get; }
    }
}