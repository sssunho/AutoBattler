using System;

namespace AutoBattler.Combat
{
    public sealed class AutoBattlerBattleUnitStat
    {
        public int MaxHealth { get; internal set; }
        public int MaxMana { get; internal set; }
        public float MoveSpeed { get; internal set; }
        public int AttackDamage { get; internal set; }
        public int AttackRange { get; internal set; }
        public float AttackAnimationDuration { get; internal set; }
        public float AttackHitNormalizedTime { get; internal set; }
        public float AttackCooldownDuration { get; internal set; }

        public AutoBattlerBattleUnitStat(
            int maxHealth,
            int maxMana,
            float moveSpeed,
            int attackDamage,
            int attackRange,
            float attackAnimationDuration,
            float attackHitNormalizedTime,
            float attackCooldownDuration)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            if (maxMana < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMana));
            }

            if (moveSpeed < 0f || float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(moveSpeed));
            }

            if (attackDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackDamage));
            }

            if (attackRange < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackRange));
            }

            if (attackAnimationDuration <= 0f
                || float.IsNaN(attackAnimationDuration)
                || float.IsInfinity(attackAnimationDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(attackAnimationDuration));
            }

            if (attackHitNormalizedTime <= 0f
                || attackHitNormalizedTime > 1f
                || float.IsNaN(attackHitNormalizedTime)
                || float.IsInfinity(attackHitNormalizedTime))
            {
                throw new ArgumentOutOfRangeException(nameof(attackHitNormalizedTime));
            }

            if (attackCooldownDuration < 0f
                || float.IsNaN(attackCooldownDuration)
                || float.IsInfinity(attackCooldownDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(attackCooldownDuration));
            }

            MaxHealth = maxHealth;
            MaxMana = maxMana;
            MoveSpeed = moveSpeed;
            AttackDamage = attackDamage;
            AttackRange = attackRange;
            AttackAnimationDuration = attackAnimationDuration;
            AttackHitNormalizedTime = attackHitNormalizedTime;
            AttackCooldownDuration = attackCooldownDuration;
        }

        public AutoBattlerBattleUnitStat(AutoBattlerBattleUnitStat source)
            : this(
                source != null ? source.MaxHealth : throw new ArgumentNullException(nameof(source)),
                source.MaxMana,
                source.MoveSpeed,
                source.AttackDamage,
                source.AttackRange,
                source.AttackAnimationDuration,
                source.AttackHitNormalizedTime,
                source.AttackCooldownDuration)
        {
        }

        internal bool IsValid()
        {
            return MaxHealth > 0
                && MaxMana >= 0
                && MoveSpeed >= 0f
                && !float.IsNaN(MoveSpeed)
                && !float.IsInfinity(MoveSpeed)
                && AttackDamage >= 0
                && AttackRange >= 0
                && AttackAnimationDuration > 0f
                && !float.IsNaN(AttackAnimationDuration)
                && !float.IsInfinity(AttackAnimationDuration)
                && AttackHitNormalizedTime > 0f
                && AttackHitNormalizedTime <= 1f
                && !float.IsNaN(AttackHitNormalizedTime)
                && !float.IsInfinity(AttackHitNormalizedTime)
                && AttackCooldownDuration >= 0f
                && !float.IsNaN(AttackCooldownDuration)
                && !float.IsInfinity(AttackCooldownDuration);
        }
    }
}
