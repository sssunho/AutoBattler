using System;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    sealed class BattleTestUnit
    {
        public BattleUnitStats Stats { get; }
        public BattleSkillDefinition BasicAttack { get; }

        public BattleTestUnit(MercenaryClass unitClass, int maxHealth, int attackDamage,
            int movementPerTurn, int attackRange, int criticalChance,
            AttackAttribute attackAttribute = AttackAttribute.Physical, int physicalDefense = 0,
            int waterDefense = 0, int fireDefense = 0, int electricityDefense = 0, int poisonDefense = 0,
            int initiative = 0, int actionPointCost = 2)
        {
            Stats = new BattleUnitStats(unitClass, maxHealth, movementPerTurn, criticalChance,
                physicalDefense, waterDefense, fireDefense, electricityDefense, poisonDefense, initiative);
            BasicAttack = new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit,
                SkillRangeSource.Own, 1, attackRange, actionPointCost, damage: attackDamage, attackAttribute: attackAttribute);
        }

        BattleTestUnit(BattleUnitStats stats, BattleSkillDefinition basicAttack)
        {
            Stats = stats;
            BasicAttack = basicAttack;
        }

        public static BattleTestUnit ForClass(MercenaryClass unitClass)
        {
            return new BattleTestUnit(BattleUnitStats.ForClass(unitClass), BattleTestSkills.ForClass(unitClass).BasicAttack);
        }

        public SkillSet CreateSkills(MercenarySpecialization specialization = MercenarySpecialization.None)
        {
            try
            {
                return BattleTestSkills.ForClass(Stats.UnitClass, specialization, BasicAttack);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    static class BattleTestUnitExtensions
    {
        public static bool TrySpawnUnit(this BattleSimulation simulation, BattleTeam team,
            HexCoordinate coordinate, BattleTestUnit definition, HexDirection facing, out HexCharacter unit,
            TraitCollection traits = null, BattleTraitSettings traitSettings = null,
            MercenarySpecialization specialization = MercenarySpecialization.None)
        {
            return simulation.TrySpawnUnit(team, coordinate, definition.Stats, facing, out unit,
                traits, traitSettings, specialization, definition.CreateSkills(specialization));
        }

        public static BattleSpawnInput CreateSpawnInput(HexCharacterView prefab, BattleTeam team,
            BattleTestUnit definition, HexCoordinate coordinate, HexDirection facing,
            TraitCollection traits = null, BattleTraitSettings traitSettings = null,
            MercenarySpecialization specialization = MercenarySpecialization.None)
        {
            return new BattleSpawnInput(prefab, team, definition.Stats, coordinate, facing,
                traits, traitSettings, specialization, definition.CreateSkills(specialization));
        }

        public static BattleSpawnInput CreateSpawnInput(HexCharacterView prefab, BattleTeam team,
            BattleUnitStats stats, HexCoordinate coordinate, HexDirection facing,
            TraitCollection traits = null, BattleTraitSettings traitSettings = null,
            MercenarySpecialization specialization = MercenarySpecialization.None, SkillSet skills = null)
        {
            return new BattleSpawnInput(prefab, team, stats, coordinate, facing,
                traits, traitSettings, specialization, skills);
        }
    }
}
