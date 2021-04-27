namespace Rambler.Server.Utility
{
    using System.Collections.Generic;

    public static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue item)
        {
            if (dictionary.ContainsKey(key)) { return false; }
            dictionary.Add(key, item);
            return true;
        }
    }
}
