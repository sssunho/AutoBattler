using System;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleStatsTests
    {
        [TestCase(MercenaryClass.Warrior, 10, 2, 1, 5, 1)]
        [TestCase(MercenaryClass.Rogue, 8, 4, 1, 15, 0)]
        [TestCase(MercenaryClass.Archer, 8, 2, 3, 10, 0)]
        public void ClassDefaultsMatchInitialTestValues(MercenaryClass unitClass, int health,
            int movement, int range, int criticalChance, int physicalDefense)
        {
            BattleUnitStats stats = BattleUnitStats.ForClass(unitClass);
            BattleSkillDefinition attack = BattleTestSkills.ForClass(unitClass).BasicAttack;

            Assert.That(stats.UnitClass, Is.EqualTo(unitClass));
            Assert.That(stats.MaxHealth, Is.EqualTo(health));
            Assert.That(attack.Damage, Is.EqualTo(3));
            Assert.That(stats.MovementPerTurn, Is.EqualTo(movement));
            Assert.That(attack.MaxRange, Is.EqualTo(range));
            Assert.That(stats.CriticalChance, Is.EqualTo(criticalChance));
            Assert.That(attack.AttackAttribute, Is.EqualTo(AttackAttribute.Physical));
            Assert.That(stats.PhysicalDefense, Is.EqualTo(physicalDefense));
            Assert.That(stats.WaterDefense, Is.Zero);
            Assert.That(stats.FireDefense, Is.Zero);
            Assert.That(stats.ElectricityDefense, Is.Zero);
            Assert.That(stats.PoisonDefense, Is.Zero);
        }

        [Test]
        public void CustomStatsKeepIndependentAttributes()
        {
            BattleUnitStats stats = new BattleUnitStats(MercenaryClass.Archer, 19, 0, 100,
                physicalDefense: 1, waterDefense: 2, fireDefense: 3, electricityDefense: 4, poisonDefense: 5);

            Assert.That(stats.MaxHealth, Is.EqualTo(19));
            Assert.That(stats.MovementPerTurn, Is.Zero);
            Assert.That(stats.CriticalChance, Is.EqualTo(100));
            Assert.That(stats.PhysicalDefense, Is.EqualTo(1));
            Assert.That(stats.WaterDefense, Is.EqualTo(2));
            Assert.That(stats.FireDefense, Is.EqualTo(3));
            Assert.That(stats.ElectricityDefense, Is.EqualTo(4));
            Assert.That(stats.PoisonDefense, Is.EqualTo(5));
        }

        [TestCase("maxHealth", 0)]
        [TestCase("maxHealth", -1)]
        [TestCase("movementPerTurn", -1)]
        [TestCase("criticalChance", -1)]
        [TestCase("criticalChance", 101)]
        [TestCase("physicalDefense", -1)]
        [TestCase("waterDefense", -1)]
        [TestCase("fireDefense", -1)]
        [TestCase("electricityDefense", -1)]
        [TestCase("poisonDefense", -1)]
        public void InvalidStatValuesAreRejected(string parameter, int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleUnitStats(MercenaryClass.Archer,
                parameter == "maxHealth" ? value : 10,
                parameter == "movementPerTurn" ? value : 2,
                parameter == "criticalChance" ? value : 0,
                physicalDefense: parameter == "physicalDefense" ? value : 0,
                waterDefense: parameter == "waterDefense" ? value : 0,
                fireDefense: parameter == "fireDefense" ? value : 0,
                electricityDefense: parameter == "electricityDefense" ? value : 0,
                poisonDefense: parameter == "poisonDefense" ? value : 0));
        }

        [Test]
        public void InvalidClassAndDefenseAttributeAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleUnitStats.ForClass((MercenaryClass)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleUnitStats((MercenaryClass)999, 10, 2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleUnitStats.ForClass(MercenaryClass.Warrior)
                .GetDefense((AttackAttribute)999));
        }

        [Test]
        public void InvalidSpawnStatsAndFacingDoNotReserveCellOrConsumeId()
        {
            BattleSimulation simulation = new BattleSimulation(2, 1, randomSeed: 7);
            BattleUnitStats stats = BattleUnitStats.ForClass(MercenaryClass.Rogue);
            SkillSet skills = BattleTestSkills.ForClass(MercenaryClass.Rogue);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, null, HexDirection.East,
                out HexCharacter missingStats, skills: skills), Is.False);
            Assert.That(missingStats, Is.Null);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, stats, (HexDirection)999,
                out HexCharacter invalidFacing, skills: skills), Is.False);
            Assert.That(invalidFacing, Is.Null);
            Assert.That(simulation.Board.IsOccupied(default), Is.False);
            Assert.That(simulation.Board.Units.Count, Is.Zero);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East,
                out HexCharacter unit, skills: skills), Is.True);
            Assert.That(unit.Id, Is.EqualTo(1));
        }

        [Test]
        public void ZeroAndMaximumStatBoundariesAreAccepted()
        {
            Assert.DoesNotThrow(() => new BattleUnitStats(MercenaryClass.Archer, int.MaxValue,
                0, 0,
                int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));
            Assert.DoesNotThrow(() => new BattleUnitStats(MercenaryClass.Rogue, 1, int.MaxValue, 100));
        }

        [TestCase(MercenaryClass.Warrior, 10, 2)]
        [TestCase(MercenaryClass.Rogue, 8, 4)]
        [TestCase(MercenaryClass.Archer, 8, 2)]
        public void RegistrationUsesClassHealthAndEveryOwnTurnResetsClassMovement(
            MercenaryClass unitClass, int health, int movement)
        {
            BattleSimulation simulation = new BattleSimulation(6, 2, randomSeed: 7);
            BattleUnitStats stats = BattleUnitStats.ForClass(unitClass);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.NorthWest,
                out HexCharacter player, skills: BattleTestSkills.ForClass(unitClass)), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(5, 1),
                BattleUnitStats.ForClass(MercenaryClass.Warrior), HexDirection.West, out _,
                skills: BattleTestSkills.ForClass(MercenaryClass.Warrior)), Is.True);

            Assert.That(player.Stats, Is.SameAs(stats));
            Assert.That(player.Health, Is.EqualTo(health));
            Assert.That(player.MaxHealth, Is.EqualTo(health));
            Assert.That(player.MovementRemaining, Is.Zero);
            Assert.That(player.ActionPoints, Is.EqualTo(4));
            Assert.That(player.Facing, Is.EqualTo(HexDirection.NorthWest));

            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(player.MovementRemaining, Is.EqualTo(movement));
            Assert.That(player.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(1, 0)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(player.MovementRemaining, Is.EqualTo(movement - 1));
            Assert.That(simulation.TryEndTurn(player.Id), Is.True);
            BattleTestUtility.AdvanceToTurn(simulation, player.Id);
            Assert.That(player.MovementRemaining, Is.EqualTo(movement));
            Assert.That(player.ActionPoints, Is.EqualTo(8));
        }
    }
}
