using System;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat
{
    static class BattleDamageResolver
    {
        public static BattleResolvedDamage Resolve(HexCharacter attacker, HexCharacter target,
            BattleAttackContext attack, bool primary, int fireBonus, int electricBonus,
            Random random, int criticalDamageBonus)
        {
            BattleRelativeDirection direction = BattleFacing.Classify(target.Facing, target.Coordinate, attacker.Coordinate);
            bool critical = primary && ((attacker.Traits.Has(TraitType.RearGuaranteedCritical)
                && direction == BattleRelativeDirection.Rear) || RollChance(random, attacker.Stats.CriticalChance));
            bool fourElement = primary && attack.Kind == BattleAttackKind.Basic && attacker.Traits.Has(TraitType.FourElementBasicAttack);
            AttackAttribute? attribute = !primary ? AttackAttribute.Electricity : fourElement ? (AttackAttribute?)null : attacker.Skills.BasicAttack.AttackAttribute;
            long physical = 0, water = 0, fire = fireBonus, electricity = electricBonus, poison = 0, untyped = 0;
            bool negated = false;
            long criticalBonus = (long)criticalDamageBonus + attacker.Equipment.CriticalDamageBonus;
            if (fourElement)
            {
                water = 1;
                fire += 1;
                electricity += 1;
                poison = 1;
                untyped = critical ? criticalBonus : 0;
            }
            else if (primary)
            {
                long amount = (long)attacker.Skills.BasicAttack.Damage + attack.AdditionalDamage + (critical ? criticalBonus : 0);
                switch (attribute.Value)
                {
                    case AttackAttribute.Physical:
                        physical = amount + attacker.Equipment.PhysicalAttackBonus;
                        break;
                    case AttackAttribute.Water:
                        water = amount;
                        break;
                    case AttackAttribute.Fire:
                        fire += amount;
                        break;
                    case AttackAttribute.Electricity:
                        electricity += amount;
                        break;
                    case AttackAttribute.Poison:
                        poison = amount;
                        break;
                    case AttackAttribute.Untyped:
                        untyped = amount;
                        break;
                }
                if (attribute == AttackAttribute.Physical && target.Traits.Has(TraitType.DirectionalPhysicalNegation))
                {
                    float chance = direction == BattleRelativeDirection.Front ? target.TraitSettings.FrontNegationChance
                        : direction == BattleRelativeDirection.Side ? target.TraitSettings.SideNegationChance : target.TraitSettings.RearNegationChance;
                    negated = RollChance(random, chance);
                    if (negated)
                    {
                        physical = 0;
                    }
                }
            }
            BattleDamageBreakdown breakdown = new BattleDamageBreakdown(
                Math.Max(0L, physical - target.Stats.PhysicalDefense), Math.Max(0L, water - target.Stats.WaterDefense),
                Math.Max(0L, fire - target.Stats.FireDefense), Math.Max(0L, electricity - target.Stats.ElectricityDefense),
                Math.Max(0L, poison - target.Stats.PoisonDefense), untyped);
            return new BattleResolvedDamage(attribute, breakdown, critical, negated);
        }

        static bool RollChance(Random random, int chance)
        {
            return chance == 100 || (chance > 0 && random.Next(100) < chance);
        }

        static bool RollChance(Random random, float chance)
        {
            return chance == 1f || (chance > 0f && random.NextDouble() < chance);
        }
    }
}
