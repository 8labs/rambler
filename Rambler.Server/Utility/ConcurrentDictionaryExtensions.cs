namespace Rambler.Server.Utility
{
    using System.Collections.Concurrent;

    public static class ConcurrentDictionaryExtensions
    {
        public static TValue Get<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return default(TValue);
        }
    }
}
