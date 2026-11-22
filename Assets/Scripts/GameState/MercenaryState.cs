using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.GameState
{
    public sealed class MercenaryState
    {
        readonly List<MercenaryInstance> _mercenaries;
        readonly ReadOnlyCollection<MercenaryInstance> _mercenaryView;
        int _level;
        int _experience;
        int _nextInstanceId;

        public IReadOnlyList<MercenaryInstance> Mercenaries => _mercenaryView;
        public int Level => _level;
        public int Experience => _experience;
        public int NextInstanceId => _nextInstanceId;

        public MercenaryState(int initialExperience)
        {
            if (initialExperience < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialExperience));
            }

            _mercenaries = new List<MercenaryInstance>();
            _mercenaryView = _mercenaries.AsReadOnly();
            _level = 1;
            _experience = initialExperience;
            _nextInstanceId = 1;
        }

        public MercenaryInstance AddMercenary(string name, MercenaryClass unitClass, int currentHP)
        {
            int nextInstanceId = checked(_nextInstanceId + 1);
            var mercenary = new MercenaryInstance(_nextInstanceId, name, unitClass, currentHP);
            _mercenaries.Add(mercenary);
            _nextInstanceId = nextInstanceId;
            return mercenary;
        }

        public bool TryRemoveMercenary(int instanceId)
        {
            if (instanceId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(instanceId));
            }

            for (int i = 0; i < _mercenaries.Count; i++)
            {
                if (_mercenaries[i].InstanceId == instanceId)
                {
                    _mercenaries.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void SetProgress(int level, int experience)
        {
            if (level < 1 || level > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (experience < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(experience));
            }

            _level = level;
            _experience = experience;
        }
    }
}
