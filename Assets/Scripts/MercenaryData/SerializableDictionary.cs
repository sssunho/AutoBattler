using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [Serializable]
    public sealed class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [Serializable]
        struct Entry
        {
            [SerializeField]
            TKey _key;

            [SerializeField]
            TValue _value;

            public TKey Key => _key;
            public TValue Value => _value;
        }

        [SerializeField]
        List<Entry> _entries = new List<Entry>();

        [NonSerialized]
        Dictionary<TKey, TValue> _lookup;

        public int EntryCount => _entries != null ? _entries.Count : 0;

        public TKey GetKey(int index)
        {
            return _entries[index].Key;
        }

        public TValue GetValue(int index)
        {
            return _entries[index].Value;
        }

        public bool Validate(out string error)
        {
            Invalidate();

            if (_entries == null)
            {
                error = "Entries must not be null.";
                return false;
            }

            Dictionary<TKey, TValue> lookup = new Dictionary<TKey, TValue>(_entries.Count);

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];

                if (entry.Key is null)
                {
                    error = $"Entries[{i}].Key must not be null.";
                    return false;
                }

                if (lookup.ContainsKey(entry.Key))
                {
                    error = $"Entries[{i}].Key duplicates '{entry.Key}'.";
                    return false;
                }

                lookup.Add(entry.Key, entry.Value);
            }

            _lookup = lookup;
            error = null;
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_lookup == null || key is null)
            {
                value = default;
                return false;
            }

            return _lookup.TryGetValue(key, out value);
        }

        public void Invalidate()
        {
            _lookup = null;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Invalidate();
        }
    }
}
