using System;
using AutoBattler.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class BattleUnit
    {
        public int Id { get; }
        public Team Team { get; }
        public HexCoordinate Coordinate { get; internal set; }
        public int AttackRange { get; }
        public float MoveSpeed { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; internal set; }
        public int AttackDamage { get; }
        public float AttackAnimationDuration { get; }
        public float AttackHitNormalizedTime { get; }
        public float AttackCooldownDuration { get; }
        public float MoveGauge { get; internal set; }
        public bool IsAlive { get; internal set; }
        public BattleAttackPhase AttackPhase { get; internal set; }
        public BattleUnit AttackTarget { get; internal set; }
        public float AttackElapsed { get; internal set; }
        public bool IsAttacking => AttackPhase != BattleAttackPhase.None;
        public BattleUnit Target { get; internal set; }

        public BattleUnit(
            int id,
            Team team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed)
            : this(id, team, coordinate, attackRange, moveSpeed, 100, 10, 1.6f, 0.4f, 0.4f)
        {
        }

        public BattleUnit(
            int id,
            Team team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            int maxHealth,
            int attackDamage)
            : this(
                id,
                team,
                coordinate,
                attackRange,
                moveSpeed,
                maxHealth,
                attackDamage,
                1.6f,
                0.4f,
                0.4f)
        {
        }

        public BattleUnit(
            int id,
            Team team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            int maxHealth,
            int attackDamage,
            float attackAnimationDuration,
            float attackHitNormalizedTime,
            float attackCooldownDuration)
        {
            if (attackRange < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackRange));
            }

            if (moveSpeed < 0f || float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(moveSpeed));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            if (attackDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackDamage));
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

            Id = id;
            Team = team;
            Coordinate = coordinate;
            AttackRange = attackRange;
            MoveSpeed = moveSpeed;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            AttackDamage = attackDamage;
            AttackAnimationDuration = attackAnimationDuration;
            AttackHitNormalizedTime = attackHitNormalizedTime;
            AttackCooldownDuration = attackCooldownDuration;
            MoveGauge = 0f;
            IsAlive = true;
        }

        internal void ClearAttackState()
        {
            AttackPhase = BattleAttackPhase.None;
            AttackTarget = null;
            AttackElapsed = 0f;
        }
    }
}
