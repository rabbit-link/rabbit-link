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
            Expression<Action<ILinkMessageSerializer>> expr = x => x.Deserialize<object>(default(ILinkMessage<byte[]>));
            DeserializeMethod = ((MethodCallExpression) expr.Body)
                .Method
                .GetGenericMethodDefinition();
        }


        public static ILinkMessage<object> Deserialize(this ILinkMessageSerializer @this, Type bodyType,
            ILinkMessage<byte[]> message)
        {
            var genericMethod = DeserializeMethod.MakeGenericMethod(bodyType);
            return (ILinkMessage<object>) genericMethod.Invoke(@this, new object[] {message});
        }
    }
}