using System;
using UnityEngine;

namespace AutoBattler.Combat.Unity
{
    [Serializable]
    public sealed class AutoBattlerUnitStat
    {
        [SerializeField]
        [Min(1)]
        int _maxHealth = 100;

        [SerializeField]
        [Min(0)]
        int _maxMana = 100;

        [SerializeField]
        [Min(0f)]
        float _moveSpeed = 1f;

        [SerializeField]
        [Min(0)]
        int _attackDamage = 10;

        [SerializeField]
        [Min(0)]
        int _attackRange = 1;

        [SerializeField]
        [Min(0.0001f)]
        float _attackAnimationDuration = 1.6f;

        [SerializeField]
        [Range(0.0001f, 1f)]
        float _attackHitNormalizedTime = 0.4f;

        [SerializeField]
        [Min(0f)]
        float _attackCooldownDuration = 0.4f;

        public int MaxHealth => _maxHealth;
        public int MaxMana => _maxMana;
        public float MoveSpeed => _moveSpeed;
        public int AttackDamage => _attackDamage;
        public int AttackRange => _attackRange;
        public float AttackAnimationDuration => _attackAnimationDuration;
        public float AttackHitNormalizedTime => _attackHitNormalizedTime;
        public float AttackCooldownDuration => _attackCooldownDuration;

        internal bool IsValid()
        {
            return _maxHealth > 0
                && _maxMana >= 0
                && _moveSpeed >= 0f
                && !float.IsNaN(_moveSpeed)
                && !float.IsInfinity(_moveSpeed)
                && _attackDamage >= 0
                && _attackRange >= 0
                && _attackAnimationDuration > 0f
                && !float.IsNaN(_attackAnimationDuration)
                && !float.IsInfinity(_attackAnimationDuration)
                && _attackHitNormalizedTime > 0f
                && _attackHitNormalizedTime <= 1f
                && !float.IsNaN(_attackHitNormalizedTime)
                && !float.IsInfinity(_attackHitNormalizedTime)
                && _attackCooldownDuration >= 0f
                && !float.IsNaN(_attackCooldownDuration)
                && !float.IsInfinity(_attackCooldownDuration);
        }

        internal AutoBattlerBattleUnitStat CreateBattleUnitStat()
        {
            return new AutoBattlerBattleUnitStat(
                _maxHealth,
                _maxMana,
                _moveSpeed,
                _attackDamage,
                _attackRange,
                _attackAnimationDuration,
                _attackHitNormalizedTime,
                _attackCooldownDuration);
        }
    }
}
