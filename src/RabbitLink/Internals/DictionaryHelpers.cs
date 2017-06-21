#region Usings

using System.Collections.Generic;

#endregion

namespace RabbitLink.Internals
{
    internal static class DictionaryHelpers
    {
        public static TValue GetOrNull<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key)
            where TValue : class
        {
            return @this.GetOrDefault(key);
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key)
        {
            if (@this.TryGetValue(key, out var value))
            {
                return value;
            }
         
            return default(TValue);
        }
    }
}