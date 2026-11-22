using System;

namespace RaggedBanner.Combat
{
    public readonly struct BattleDamageBreakdown
    {
        public long Physical { get; }
        public long Water { get; }
        public long Fire { get; }
        public long Electricity { get; }
        public long Poison { get; }
        public long Untyped { get; }
        public long Total { get; }

        public BattleDamageBreakdown(long physical = 0, long water = 0, long fire = 0,
            long electricity = 0, long poison = 0, long untyped = 0)
        {
            if (physical < 0 || water < 0 || fire < 0 || electricity < 0 || poison < 0 || untyped < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physical), "Damage components must be nonnegative.");
            }

            Physical = physical;
            Water = water;
            Fire = fire;
            Electricity = electricity;
            Poison = poison;
            Untyped = untyped;
            Total = checked(physical + water + fire + electricity + poison + untyped);
        }

        public long GetDamage(AttackAttribute attribute)
        {
            switch (attribute)
            {
                case AttackAttribute.Physical:
                    return Physical;
                case AttackAttribute.Water:
                    return Water;
                case AttackAttribute.Fire:
                    return Fire;
                case AttackAttribute.Electricity:
                    return Electricity;
                case AttackAttribute.Poison:
                    return Poison;
                case AttackAttribute.Untyped:
                    return Untyped;
                default:
                    throw new ArgumentOutOfRangeException(nameof(attribute));
            }
        }

        internal static BattleDamageBreakdown Single(AttackAttribute attribute, long damage)
        {
            switch (attribute)
            {
                case AttackAttribute.Physical:
                    return new BattleDamageBreakdown(physical: damage);
                case AttackAttribute.Water:
                    return new BattleDamageBreakdown(water: damage);
                case AttackAttribute.Fire:
                    return new BattleDamageBreakdown(fire: damage);
                case AttackAttribute.Electricity:
                    return new BattleDamageBreakdown(electricity: damage);
                case AttackAttribute.Poison:
                    return new BattleDamageBreakdown(poison: damage);
                case AttackAttribute.Untyped:
                    return new BattleDamageBreakdown(untyped: damage);
                default:
                    throw new ArgumentOutOfRangeException(nameof(attribute));
            }
        }
    }
}
