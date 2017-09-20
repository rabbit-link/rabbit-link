using System;
using System.Linq.Expressions;
using System.Reflection;
using RabbitLink.Messaging;

namespace RabbitLink.Serialization
{
    /// <summary>
    /// Helper for <see cref="ILinkSerializer"/>
    /// </summary>
    internal static class LinkSerializerExtensions
    {
        private static readonly MethodInfo DeserializeMethod;

        static LinkSerializerExtensions()
        {
            Expression<Action<ILinkSerializer>> expr = x => x.Deserialize<object>(default(byte[]), default(LinkMessageProperties));
            
            DeserializeMethod = ((MethodCallExpression)expr.Body)
                .Method
                .GetGenericMethodDefinition();
        }
        
        /// <summary>
        ///     Deserialize messsage and set properties
        /// </summary>
        public static object Deserialize(this ILinkSerializer @this, Type bodyType,
            byte[] body, LinkMessageProperties properties)
        {
            var genericMethod = DeserializeMethod.MakeGenericMethod(bodyType);
            return genericMethod.Invoke(@this, new object[] { body, properties });
        }

    }
}