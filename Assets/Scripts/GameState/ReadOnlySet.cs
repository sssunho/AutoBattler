using System.Collections;
using System.Collections.Generic;

namespace RaggedBanner.GameState
{
    internal sealed class ReadOnlySet<T> : IReadOnlyCollection<T>
    {
        readonly HashSet<T> _items;

        public int Count => _items.Count;

        public ReadOnlySet(HashSet<T> items)
        {
            _items = items;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
