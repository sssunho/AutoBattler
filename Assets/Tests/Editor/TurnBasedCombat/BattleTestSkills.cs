using System;
using System.Collections.Generic;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    static class BattleTestSkills
    {
        public static SkillSet ForClass(MercenaryClass unitClass,
            MercenarySpecialization specialization = MercenarySpecialization.None,
            BattleSkillDefinition basicAttack = null, IReadOnlyList<BattleSkillDefinition> definitions = null)
        {
            if ((uint)unitClass > (uint)MercenaryClass.Archer
                || (uint)specialization > (uint)MercenarySpecialization.SpiritArcher
                || specialization == MercenarySpecialization.Pioneer && unitClass != MercenaryClass.Warrior
                || specialization == MercenarySpecialization.Assassin && unitClass != MercenaryClass.Rogue
                || specialization == MercenarySpecialization.SpiritArcher && unitClass != MercenaryClass.Archer)
            {
                throw new ArgumentOutOfRangeException(nameof(unitClass));
            }

            if (basicAttack != null && basicAttack.Type != SkillType.BasicAttack)
            {
                throw new ArgumentException("The basic attack must use BasicAttack behavior.", nameof(basicAttack));
            }

            List<BattleSkillDefinition> granted = new List<BattleSkillDefinition>(5)
            {
                basicAttack ?? new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit,
                    SkillRangeSource.Own, 1, unitClass == MercenaryClass.Archer ? 3 : 1, 2, damage: 3)
            };
            granted.Add(Resolve(SkillType.Stealth, definitions));

            if (unitClass == MercenaryClass.Rogue)
            {
                granted.Add(Resolve(SkillType.Ambush, definitions));
            }
            else if (unitClass == MercenaryClass.Archer)
            {
                granted.Add(Resolve(SkillType.Overwatch, definitions));
            }

            if (specialization == MercenarySpecialization.Pioneer)
            {
                granted.Add(Resolve(SkillType.PullSelf, definitions));
                granted.Add(Resolve(SkillType.PullTarget, definitions));
            }
            else if (specialization == MercenarySpecialization.Assassin)
            {
                granted.Add(Resolve(SkillType.Invisibility, definitions));
                granted.Add(Resolve(SkillType.AssassinLeap, definitions));
            }

            int[] slots = new int[granted.Count];
            for (int i = 0; i < granted.Count; i++)
            {
                switch (granted[i].Type)
                {
                    case SkillType.Stealth:
                        slots[i] = 2;
                        break;
                    case SkillType.Ambush:
                    case SkillType.Overwatch:
                    case SkillType.PullSelf:
                        slots[i] = 1;
                        break;
                    case SkillType.PullTarget:
                    case SkillType.AssassinLeap:
                        slots[i] = 3;
                        break;
                    case SkillType.Invisibility:
                        slots[i] = 4;
                        break;
                }
            }

            return new SkillSet(granted.ToArray(), slots);
        }

        static BattleSkillDefinition Resolve(SkillType type, IReadOnlyList<BattleSkillDefinition> definitions)
        {
            if (definitions != null)
            {
                BattleSkillDefinition found = null;

                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i] == null)
                    {
                        throw new ArgumentException("The skill catalog contains an empty definition.", nameof(definitions));
                    }

                    if (definitions[i].Type == type)
                    {
                        if (found != null)
                        {
                            throw new ArgumentException("The skill catalog contains duplicate definitions.", nameof(definitions));
                        }

                        found = definitions[i];
                    }
                }

                return found ?? throw new ArgumentException("The skill catalog is missing a required definition.", nameof(definitions));
            }

            switch (type)
            {
                case SkillType.Stealth:
                    return new BattleSkillDefinition(type, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 2, movementCost: 2);
                case SkillType.Ambush:
                    return new BattleSkillDefinition(type, SkillTargetType.Unit, SkillRangeSource.BasicAttack, 0, 0, 2, damageBonus: 2);
                case SkillType.Overwatch:
                    return new BattleSkillDefinition(type, SkillTargetType.Direction, SkillRangeSource.BasicAttack, 0, 0, 2);
                case SkillType.PullSelf:
                case SkillType.PullTarget:
                    return new BattleSkillDefinition(type, SkillTargetType.Unit, SkillRangeSource.Own, 2, 6, 2);
                case SkillType.Invisibility:
                    return new BattleSkillDefinition(type, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 2);
                case SkillType.AssassinLeap:
                    return new BattleSkillDefinition(type, SkillTargetType.Unit, SkillRangeSource.Own, 1, 4, 3);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
