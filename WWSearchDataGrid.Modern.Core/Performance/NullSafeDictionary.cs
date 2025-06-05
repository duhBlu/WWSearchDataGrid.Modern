using System;
using System.Collections.Generic;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// Dictionary that safely handles null keys
    /// </summary>
    public class NullSafeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private bool _hasNullKey;
        private TValue _nullValue;

        public new TValue this[TKey key]
        {
            get
            {
                if (key == null)
                    return _hasNullKey ? _nullValue : default(TValue);
                return base[key];
            }
            set
            {
                if (key == null)
                {
                    _hasNullKey = true;
                    _nullValue = value;
                }
                else
                {
                    base[key] = value;
                }
            }
        }

        public new bool ContainsKey(TKey key)
        {
            if (key == null)
                return _hasNullKey;
            return base.ContainsKey(key);
        }

        public new bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                if (_hasNullKey)
                {
                    value = _nullValue;
                    return true;
                }
                else
                {
                    value = default(TValue);
                    return false;
                }
            }
            return base.TryGetValue(key, out value);
        }

        public new void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                if (_hasNullKey)
                    throw new ArgumentException("An item with the same key has already been added.");
                _hasNullKey = true;
                _nullValue = value;
            }
            else
            {
                base.Add(key, value);
            }
        }

        public new bool Remove(TKey key)
        {
            if (key == null)
            {
                var hadNull = _hasNullKey;
                _hasNullKey = false;
                _nullValue = default(TValue);
                return hadNull;
            }
            return base.Remove(key);
        }

        public new void Clear()
        {
            base.Clear();
            _hasNullKey = false;
            _nullValue = default(TValue);
        }

        public new int Count
        {
            get { return base.Count + (_hasNullKey ? 1 : 0); }
        }

        public new ICollection<TKey> Keys
        {
            get
            {
                var keys = new List<TKey>(base.Keys);
                if (_hasNullKey)
                    keys.Add(default(TKey));
                return keys;
            }
        }

        public new ICollection<TValue> Values
        {
            get
            {
                var values = new List<TValue>(base.Values);
                if (_hasNullKey)
                    values.Add(_nullValue);
                return values;
            }
        }

        public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (_hasNullKey)
                yield return new KeyValuePair<TKey, TValue>(default(TKey), _nullValue);

            foreach (var kvp in (Dictionary<TKey, TValue>)this)
                yield return kvp;
        }
    }
}
