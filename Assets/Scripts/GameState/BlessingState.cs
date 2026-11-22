using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RaggedBanner.GameState
{
    public sealed class BlessingState
    {
        readonly Dictionary<string, int> _tiersByGodId;
        readonly ReadOnlyDictionary<string, int> _tierView;
        readonly HashSet<string> _usedConditionIds;
        readonly ReadOnlySet<string> _usedConditionView;

        public IReadOnlyDictionary<string, int> TiersByGodId => _tierView;
        public IReadOnlyCollection<string> UsedConditionIds => _usedConditionView;

        public BlessingState()
        {
            _tiersByGodId = new Dictionary<string, int>(StringComparer.Ordinal);
            _tierView = new ReadOnlyDictionary<string, int>(_tiersByGodId);
            _usedConditionIds = new HashSet<string>(StringComparer.Ordinal);
            _usedConditionView = new ReadOnlySet<string>(_usedConditionIds);
        }

        public int GetTier(string godId)
        {
            ValidateId(godId, nameof(godId));
            return _tiersByGodId.TryGetValue(godId, out int tier) ? tier : 0;
        }

        public bool IsConditionUsed(string conditionId)
        {
            ValidateId(conditionId, nameof(conditionId));
            return _usedConditionIds.Contains(conditionId);
        }

        public bool TryRecordAcquisition(string godId, string conditionId)
        {
            ValidateId(godId, nameof(godId));
            ValidateId(conditionId, nameof(conditionId));

            if (_usedConditionIds.Contains(conditionId))
            {
                return false;
            }

            _tiersByGodId.TryGetValue(godId, out int tier);
            int nextTier = checked(tier + 1);
            _tiersByGodId[godId] = nextTier;
            _usedConditionIds.Add(conditionId);
            return true;
        }

        static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An ID is required.", parameterName);
            }
        }
    }
}
