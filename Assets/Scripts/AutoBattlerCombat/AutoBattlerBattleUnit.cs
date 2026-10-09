using System;
using AutoBattler.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class AutoBattlerBattleUnit
    {
        public int Id { get; }
        public AutoBattlerTeam Team { get; }
        public HexCoordinate Coordinate { get; internal set; }
        public AutoBattlerBattleUnitStat Stat { get; }
        public int AttackRange => Stat.AttackRange;
        public float MoveSpeed => Stat.MoveSpeed;
        public int MaxHealth => Stat.MaxHealth;
        public int MaxMana => Stat.MaxMana;
        public int CurrentHealth { get; internal set; }
        public int CurrentMana { get; internal set; }
        public int AttackDamage => Stat.AttackDamage;
        public float AttackAnimationDuration => Stat.AttackAnimationDuration;
        public float AttackHitNormalizedTime => Stat.AttackHitNormalizedTime;
        public float AttackCooldownDuration => Stat.AttackCooldownDuration;
        public float MoveGauge { get; internal set; }
        public bool IsAlive { get; internal set; }
        public AutoBattlerBattleAttackPhase AttackPhase { get; internal set; }
        public AutoBattlerBattleUnit AttackTarget { get; internal set; }
        public float AttackElapsed { get; internal set; }
        public bool IsAttacking => AttackPhase != AutoBattlerBattleAttackPhase.None;
        public AutoBattlerBattleUnit Target { get; internal set; }

        public AutoBattlerBattleUnit(
            int id,
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed)
            : this(id, team, coordinate, attackRange, moveSpeed, 100, 10, 1.6f, 0.4f, 0.4f)
        {
        }

        public AutoBattlerBattleUnit(
            int id,
            AutoBattlerTeam team,
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

        public AutoBattlerBattleUnit(
            int id,
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            int maxHealth,
            int attackDamage,
            float attackAnimationDuration,
            float attackHitNormalizedTime,
            float attackCooldownDuration)
            : this(
                id,
                team,
                coordinate,
                new AutoBattlerBattleUnitStat(
                    maxHealth,
                    0,
                    moveSpeed,
                    attackDamage,
                    attackRange,
                    attackAnimationDuration,
                    attackHitNormalizedTime,
                    attackCooldownDuration))
        {
        }

        public AutoBattlerBattleUnit(
            int id,
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            AutoBattlerBattleUnitStat stat)
        {
            Id = id;
            Team = team;
            Coordinate = coordinate;
            Stat = new AutoBattlerBattleUnitStat(stat ?? throw new ArgumentNullException(nameof(stat)));
            CurrentHealth = Stat.MaxHealth;
            CurrentMana = 0;
            MoveGauge = 0f;
            IsAlive = true;
        }

        internal void ClearAttackState()
        {
            AttackPhase = AutoBattlerBattleAttackPhase.None;
            AttackTarget = null;
            AttackElapsed = 0f;
        }
    }
}
