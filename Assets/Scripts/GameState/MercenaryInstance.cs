using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.GameState
{
    public sealed class MercenaryInstance
    {
        readonly int _instanceId;
        readonly string _name;
        readonly MercenaryClass _class;
        readonly Dictionary<string, string> _selectedRewardIds;
        readonly ReadOnlyDictionary<string, string> _selectedRewardView;
        int _currentHP;
        string _specializationId;

        public int InstanceId => _instanceId;
        public string Name => _name;
        public MercenaryClass Class => _class;
        public int CurrentHP => _currentHP;
        public string SpecializationId => _specializationId;
        public IReadOnlyDictionary<string, string> SelectedRewardIds => _selectedRewardView;

        internal MercenaryInstance(int instanceId, string name, MercenaryClass unitClass, int currentHP)
        {
            if (instanceId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(instanceId));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A mercenary name is required.", nameof(name));
            }

            if (!Enum.IsDefined(typeof(MercenaryClass), unitClass))
            {
                throw new ArgumentOutOfRangeException(nameof(unitClass));
            }

            if (currentHP < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentHP));
            }

            _instanceId = instanceId;
            _name = name;
            _class = unitClass;
            _currentHP = currentHP;
            _selectedRewardIds = new Dictionary<string, string>(StringComparer.Ordinal);
            _selectedRewardView = new ReadOnlyDictionary<string, string>(_selectedRewardIds);
        }

        public void SetCurrentHP(int currentHP)
        {
            if (currentHP < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentHP));
            }

            _currentHP = currentHP;
        }

        public void SelectSpecialization(string specializationId)
        {
            if (string.IsNullOrWhiteSpace(specializationId))
            {
                throw new ArgumentException("A specialization ID is required.", nameof(specializationId));
            }

            if (_specializationId != null)
            {
                throw new InvalidOperationException("A specialization has already been selected.");
            }

            _specializationId = specializationId;
        }

        public void SetSelectedReward(string slotId, string rewardId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new ArgumentException("A reward slot ID is required.", nameof(slotId));
            }

            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("A reward ID is required.", nameof(rewardId));
            }

            _selectedRewardIds[slotId] = rewardId;
        }
    }
}
