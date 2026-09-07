using AutoBattler.HexGrid;

namespace AutoBattler.Combat.Unity
{
    internal readonly struct BattlePlacement
    {
        internal Team Team { get; }
        internal HexCoordinate Coordinate { get; }
        internal int AttackRange { get; }
        internal float MoveSpeed { get; }
        internal int MaxHealth { get; }
        internal int AttackDamage { get; }
        internal float AttackAnimationDuration { get; }
        internal float AttackHitNormalizedTime { get; }
        internal float AttackCooldownDuration { get; }

        internal BattlePlacement(
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
            Team = team;
            Coordinate = coordinate;
            AttackRange = attackRange;
            MoveSpeed = moveSpeed;
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            AttackAnimationDuration = attackAnimationDuration;
            AttackHitNormalizedTime = attackHitNormalizedTime;
            AttackCooldownDuration = attackCooldownDuration;
        }
    }
}
