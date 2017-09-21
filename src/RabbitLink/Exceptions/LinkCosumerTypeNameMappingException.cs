namespace RabbitLink.Exceptions
{
    /// <summary>
    /// Fires when type name mapping not found
    /// </summary>
    public class LinkCosumerTypeNameMappingException : LinkException
    {
        /// <summary>
        /// Constructs instance when no Type header in message
        /// </summary>
        public LinkCosumerTypeNameMappingException()
            : base("Message not contains Type header")
        {
        }

        /// <summary>
        /// Constructs instance when Type for Name not found
        /// </summary>
        public LinkCosumerTypeNameMappingException(string name)
            : base($"Cannot get mapping for TypeName {name}")
        {
            Name = name;
        }

        /// <summary>
        /// Mapping name
        /// </summary>
        public string Name { get; }
    }
}
