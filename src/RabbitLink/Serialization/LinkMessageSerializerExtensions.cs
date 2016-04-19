#region Usings

using System;
using System.Linq.Expressions;
using System.Reflection;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Serialization
{
    public static class LinkMessageSerializerExtensions
    {
        private static readonly MethodInfo DeserializeMethod;

        static LinkMessageSerializerExtensions()
        {
            Expression<Action<ILinkMessageSerializer>> expr = x => x.Deserialize<object>(default(byte[]), default(LinkMessageProperties));
            DeserializeMethod = ((MethodCallExpression)expr.Body)
                .Method
                .GetGenericMethodDefinition();
        }


        public static object Deserialize(this ILinkMessageSerializer @this, Type bodyType,
            byte[] body, LinkMessageProperties properties)
        {
            var genericMethod = DeserializeMethod.MakeGenericMethod(bodyType);
            return genericMethod.Invoke(@this, new object[] { body, properties });
        }
    }
}