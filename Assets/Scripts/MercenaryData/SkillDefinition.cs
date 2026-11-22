using System;
using RaggedBanner.Combat;
using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [CreateAssetMenu(menuName = "RaggedBanner/Skill Definition", fileName = "SkillDefinition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField]
        SkillType _type;

        [SerializeField]
        SkillTargetType _targetType = SkillTargetType.Unit;

        [SerializeField]
        SkillInputMode _inputMode = SkillInputMode.Unit;

        [Header("Range")]
        [SerializeField]
        SkillRangeSource _rangeSource;

        [SerializeField]
        int _minRange = 1;

        [SerializeField]
        int _maxRange = 1;

        [Header("Cost")]
        [SerializeField]
        int _actionPointCost = 2;

        [SerializeField]
        int _movementCost;

        [SerializeField]
        int _cooldownTurns;

        [Header("Attack")]
        [SerializeField]
        int _damage = 3;

        [SerializeField]
        AttackAttribute _attackAttribute;

        [SerializeField]
        int _damageBonus;

        [Header("Spirit Imbuement")]
        [SerializeField, Min(0)]
        int _fireSpiritDamage = 1;

        [SerializeField, Min(0)]
        int _electricSpiritDamage = 1;

        public SkillType Type => _type;
        public SkillTargetType TargetType => _targetType;
        public SkillInputMode InputMode => _inputMode;
        public SkillRangeSource RangeSource => _rangeSource;
        public int MinRange => _minRange;
        public int MaxRange => _maxRange;
        public int ActionPointCost => _actionPointCost;
        public int MovementCost => _movementCost;
        public int CooldownTurns => _cooldownTurns;
        public int Damage => _damage;
        public AttackAttribute AttackAttribute => _attackAttribute;
        public int DamageBonus => _damageBonus;
        public int FireSpiritDamage => _fireSpiritDamage;
        public int ElectricSpiritDamage => _electricSpiritDamage;

        public bool Validate(out string error)
        {
            return TryCreateRuntime(out _, out error);
        }

        public bool TryCreateRuntime(out BattleSkillDefinition definition, out string error)
        {
            definition = null;

            try
            {
                definition = new BattleSkillDefinition(_type, _targetType, _rangeSource,
                    _minRange, _maxRange, _actionPointCost, _movementCost, _damage, _attackAttribute, _damageBonus,
                    _fireSpiritDamage, _electricSpiritDamage, _cooldownTurns, _inputMode);
                error = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                error = $"SkillDefinition '{name}': {exception.Message}";
                return false;
            }
        }
    }
}
