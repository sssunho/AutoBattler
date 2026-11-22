using System.Collections.Generic;

namespace RaggedBanner.Combat
{
    sealed class BattleReactionExecution
    {
        readonly List<BattleReactionRequest> _pending = new List<BattleReactionRequest>();
        readonly HashSet<(int ReactorId, int TargetId)> _opportunityUses = new HashSet<(int, int)>();
        int _index;
        int _round;

        public bool IsActive { get; private set; }
        public bool HasPending => _index < _pending.Count;

        public void Add(BattleReactionRequest request)
        {
            _pending.Add(request);
        }

        public void BeginBatch()
        {
            IsActive = true;
        }

        public bool TryTakeNext(out BattleReactionRequest request)
        {
            request = default;
            if (!HasPending)
            {
                return false;
            }

            request = _pending[_index++];
            return true;
        }

        public void ClearPending()
        {
            _pending.Clear();
            _index = 0;
        }

        public void ResetBatch()
        {
            IsActive = false;
            ClearPending();
        }

        public bool HasUsedOpportunity(int reactorId, int targetId)
        {
            return _opportunityUses.Contains((reactorId, targetId));
        }

        public void RecordOpportunity(int reactorId, int targetId)
        {
            _opportunityUses.Add((reactorId, targetId));
        }

        public void BeginRound(int round)
        {
            if (_round != round)
            {
                _opportunityUses.Clear();
                _round = round;
            }
        }
    }
}
