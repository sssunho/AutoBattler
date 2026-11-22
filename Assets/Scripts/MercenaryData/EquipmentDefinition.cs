using RaggedBanner.Combat;
using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [CreateAssetMenu(menuName = "RaggedBanner/Equipment Definition")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [SerializeField] EquipmentType _type;
        [SerializeField] string _displayName;
        [SerializeField, Min(0)] int _physicalAttackBonus;
        [SerializeField, Min(0)] int _criticalDamageBonus;
        [SerializeField, Min(0)] int _physicalDefenseBonus;
        [SerializeField, Min(0)] int _maxHealthBonus;

        public EquipmentType Type => _type;
        public string DisplayName => _displayName;

        public bool TryCreateSnapshot(out BattleEquipmentItem item, out string error)
        {
            item = default;
            if ((uint)_type > (uint)EquipmentType.Armor || string.IsNullOrWhiteSpace(_displayName)
                || _physicalAttackBonus < 0 || _criticalDamageBonus < 0 || _physicalDefenseBonus < 0 || _maxHealthBonus < 0
                || _type != EquipmentType.Longsword && _physicalAttackBonus != 0
                || _type != EquipmentType.Dagger && _criticalDamageBonus != 0
                || _type != EquipmentType.Shield && _physicalDefenseBonus != 0
                || _type != EquipmentType.Armor && _maxHealthBonus != 0)
            {
                error = "Equipment requires a valid type, name and nonnegative bonuses matching its type.";
                return false;
            }

            item = new BattleEquipmentItem(_type, _physicalAttackBonus, _criticalDamageBonus, _physicalDefenseBonus, _maxHealthBonus);
            error = null;
            return true;
        }
    }
}
