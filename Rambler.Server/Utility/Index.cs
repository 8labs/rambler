namespace Rambler.Server.Utility
{
    using System.Collections.Generic;
    using System.Linq;

    public class Index<TKey, TValue>
    {
        private readonly Dictionary<TKey, HashSet<TValue>> index = new Dictionary<TKey, HashSet<TValue>>();
        public bool Remove(TKey key)
        {
            return index.Remove(key);
        }

        public bool Remove(TKey key, TValue item)
        {
            if (index.ContainsKey(key))
            {
                return index[key].Remove(item);
            }
            return false;
        }

        public bool Add(TKey key, TValue item)
        {
            if (!index.ContainsKey(key))
            {
                index.Add(key, new HashSet<TValue>() { item });
                return true;
            }

            return index[key].Add(item);
        }

        public bool Has(TKey key, TValue item)
        {
            return index.ContainsKey(key) && index[key].Contains(item);
        }

        public IEnumerable<TValue> Get(TKey key)
        {
            if (index.ContainsKey(key))
            {
                return index[key];
            }

            return Enumerable.Empty<TValue>();
        }

        public int GetCount(TKey key)
        {
            if (index.ContainsKey(key))
            {
                return index[key].Count;
            }

            return 0;
        }
    }
}
