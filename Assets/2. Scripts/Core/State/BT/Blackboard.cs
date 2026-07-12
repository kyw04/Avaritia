using System.Collections.Generic;

namespace BT
{
    public class Blackboard
    {
        private readonly Dictionary<BBKey, object> data = new();

        public void Set<T>(BBKey key, T value) => data[key] = value;

        public T Get<T>(BBKey key)
        {
            if (!data.TryGetValue(key, out var value))
                throw new System.Collections.Generic.KeyNotFoundException($"BBKey.{key} not set in Blackboard.");
            return (T)value;
        }

        public bool Has(BBKey key) => data.ContainsKey(key);
    }
}
