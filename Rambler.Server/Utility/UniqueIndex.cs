namespace Rambler.Server.Utility
{
    using System.Collections.Generic;

    public class UniqueIndex<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> index;

        public UniqueIndex()
        {
            index = new Dictionary<TKey, TValue>();
        }

        public UniqueIndex(IEqualityComparer<TKey> comparer)
        {
            index = new Dictionary<TKey, TValue>(comparer);
        }

        public IEnumerable<TValue> Values
        {
            get { return index.Values; }
        }

        public int Count
        {
            get { return index.Count; }
        }

        public bool Remove(TKey key)
        {
            return index.Remove(key);
        }

        public bool Add(TKey key, TValue item)
        {
            return index.TryAdd(key, item);
        }

        public void Set(TKey key, TValue value)
        {
            if (index.ContainsKey(key))
            {
                index[key] = value;
            }
            else
            {
                index.Add(key, value);
            }
        }

        public bool Has(TKey key)
        {
            return index.ContainsKey(key);
        }

        public bool TryGet(TKey key, out TValue item)
        {
            return index.TryGetValue(key, out item);
        }
    }
}
