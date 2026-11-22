using System;
using System.Collections.Generic;

namespace RaggedBanner.Combat
{
    sealed class BattleTurnOrder
    {
        readonly struct Entry
        {
            public int UnitId { get; }
            public int Initiative { get; }

            public Entry(HexCharacter unit)
            {
                UnitId = unit.Id;
                Initiative = unit.Stats.Initiative;
            }
        }

        static readonly Comparison<Entry> _compareEntries = CompareEntries;

        readonly List<Entry> _entries = new List<Entry>();
        readonly Random _random;

        int _index = -1;
        int _roundNumber;

        public int RoundNumber => _roundNumber;
        public int? CurrentUnitId => _index >= 0 && _index < _entries.Count ? _entries[_index].UnitId : (int?)null;
        internal int Count => _entries.Count;

        public BattleTurnOrder(int? randomSeed)
        {
            _random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
        }

        public void StartRound(BattleBoard board)
        {
            _entries.Clear();

            foreach (HexCharacter unit in board.Units)
            {
                _entries.Add(new Entry(unit));
            }

            _entries.Sort(_compareEntries);

            int groupStart = 0;

            while (groupStart < _entries.Count)
            {
                int groupEnd = groupStart + 1;

                while (groupEnd < _entries.Count
                    && _entries[groupEnd].Initiative == _entries[groupStart].Initiative)
                {
                    groupEnd++;
                }

                for (int i = groupEnd - 1; i > groupStart; i--)
                {
                    int other = groupStart + _random.Next(i - groupStart + 1);
                    Entry entry = _entries[i];
                    _entries[i] = _entries[other];
                    _entries[other] = entry;
                }

                groupStart = groupEnd;
            }

            _roundNumber++;
            _index = 0;
        }

        public void MoveToNextTurn(BattleBoard board)
        {
            _index++;

            while (_index < _entries.Count && !board.TryGetUnit(_entries[_index].UnitId, out _))
            {
                _index++;
            }

            if (_index >= _entries.Count)
            {
                StartRound(board);
            }
        }

        internal int GetUnitId(int index)
        {
            return _entries[index].UnitId;
        }

        static int CompareEntries(Entry left, Entry right)
        {
            int initiativeOrder = right.Initiative.CompareTo(left.Initiative);
            return initiativeOrder != 0 ? initiativeOrder : left.UnitId.CompareTo(right.UnitId);
        }
    }
}
