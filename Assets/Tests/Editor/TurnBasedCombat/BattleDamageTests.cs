using System;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleDamageTests
    {
        [TestCase(AttackAttribute.Physical, 7)]
        [TestCase(AttackAttribute.Water, 6)]
        [TestCase(AttackAttribute.Fire, 5)]
        [TestCase(AttackAttribute.Electricity, 4)]
        [TestCase(AttackAttribute.Poison, 3)]
        [TestCase(AttackAttribute.Untyped, 8)]
        public void AttackUsesOnlyItsMatchingDefense(AttackAttribute attribute, int expectedDamage)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 20, 8, 2, 3, 0, attribute);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 20, 3, 2, 3, 0,
                physicalDefense: 1, waterDefense: 2, fireDefense: 3, electricityDefense: 4, poisonDefense: 5);
            BattleSimulation simulation = CreateBattle(attackerStats, targetStats,
                out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.TargetId, Is.EqualTo(target.Id));
            Assert.That(result.AttackAttribute, Is.EqualTo(attribute));
            Assert.That(result.IsCritical, Is.False);
            Assert.That(result.WasNegated, Is.False);
            Assert.That(result.Damage, Is.EqualTo(expectedDamage));
            Assert.That(result.RemainingHealth, Is.EqualTo(20 - expectedDamage));
            Assert.That(target.Health, Is.EqualTo(result.RemainingHealth));
        }

        [TestCase(AttackAttribute.Physical, 0, 3)]
        [TestCase(AttackAttribute.Physical, 100, 5)]
        [TestCase(AttackAttribute.Water, 0, 3)]
        [TestCase(AttackAttribute.Water, 100, 5)]
        [TestCase(AttackAttribute.Fire, 0, 3)]
        [TestCase(AttackAttribute.Fire, 100, 5)]
        [TestCase(AttackAttribute.Electricity, 0, 3)]
        [TestCase(AttackAttribute.Electricity, 100, 5)]
        [TestCase(AttackAttribute.Poison, 0, 3)]
        [TestCase(AttackAttribute.Poison, 100, 5)]
        [TestCase(AttackAttribute.Untyped, 0, 5)]
        [TestCase(AttackAttribute.Untyped, 100, 7)]
        public void CriticalChanceEndpointsApplyTwoDamageBeforeDefense(
            AttackAttribute attribute, int criticalChance, int expectedDamage)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 20, 5, 2, 3,
                criticalChance, attribute);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 20, 3, 2, 3, 0,
                physicalDefense: 2, waterDefense: 2, fireDefense: 2, electricityDefense: 2, poisonDefense: 2);
            BattleSimulation simulation = CreateBattle(attackerStats, targetStats,
                out HexCharacter attacker, out HexCharacter target);

            for (int i = 0; i < 2; i++)
            {
                BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);
                Assert.That(result.IsCritical, Is.EqualTo(criticalChance == 100));
                Assert.That(result.Damage, Is.EqualTo(expectedDamage));
            }
        }

        [TestCase(0, 5, 0)]
        [TestCase(0, 6, 0)]
        [TestCase(100, 6, 1)]
        [TestCase(100, 7, 0)]
        [TestCase(100, 100, 0)]
        public void DefenseClampsDamageToZeroAndAttackStillConsumesApAndCompletes(
            int criticalChance, int defense, int expectedDamage)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 20, 5, 2, 3, criticalChance);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 20, 3, 2, 3, 0,
                physicalDefense: defense);
            BattleSimulation simulation = CreateBattle(attackerStats, targetStats,
                out HexCharacter attacker, out HexCharacter target);

            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);
            Assert.That(attacker.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(attacker.ActionPoints, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(20));
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(expectedDamage));
            Assert.That(result.IsCritical, Is.EqualTo(criticalChance == 100));
            Assert.That(result.WasNegated, Is.False);
            Assert.That(result.TargetDied, Is.False);
            Assert.That(target.Health, Is.EqualTo(20 - expectedDamage));
            Assert.That(attacker.ActionPoints, Is.EqualTo(2));
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(attacker.Id));
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
        }

        [TestCase(AttackAttribute.Untyped, 0)]
        [TestCase(AttackAttribute.Untyped, 100)]
        public void UntypedDamageIgnoresEveryDefenseIncludingMaximumValues(
            AttackAttribute attribute, int criticalChance)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 20, 5, 2, 3,
                criticalChance, attribute);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 20, 3, 2, 3, 0,
                physicalDefense: int.MaxValue, waterDefense: int.MaxValue, fireDefense: int.MaxValue,
                electricityDefense: int.MaxValue, poisonDefense: int.MaxValue);
            BattleSimulation simulation = CreateBattle(attackerStats, targetStats,
                out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.Damage, Is.EqualTo(criticalChance == 100 ? 7 : 5));
            Assert.That(result.WasNegated, Is.False);
        }

        [TestCase(AttackAttribute.Physical, int.MaxValue, 20, 2)]
        [TestCase(AttackAttribute.Physical, 0, int.MaxValue, int.MaxValue)]
        [TestCase(AttackAttribute.Untyped, int.MaxValue, 20, 20)]
        public void CriticalAdditionUsesWideIntermediateAndActualLossNeverExceedsHealth(
            AttackAttribute attribute, int defense, int health, int expectedDamage)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 20, int.MaxValue,
                2, 3, 100, attribute);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, health, 3, 2, 3, 0,
                physicalDefense: defense);
            BattleSimulation simulation = CreateBattle(attackerStats, targetStats,
                out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.IsCritical, Is.True);
            Assert.That(result.Damage, Is.EqualTo(expectedDamage));
            Assert.That(result.RemainingHealth, Is.EqualTo(health - expectedDamage));
            Assert.That(result.TargetDied, Is.EqualTo(expectedDamage == health));
            Assert.That(target.Health, Is.EqualTo(health - expectedDamage));
            Assert.That(simulation.Board.IsOccupied(target.Coordinate), Is.EqualTo(expectedDamage < health));
            Assert.That(simulation.State,
                Is.EqualTo(expectedDamage == health ? BattleState.Waiting : BattleState.Running));
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
        }

        [Test]
        public void CriticalRollsUseAttackersOwnChanceAndSeparateSeededSequence()
        {
            const int seed = 47;
            BattleRules rules = new BattleRules(startingActionPoints: 8);
            BattleSimulation simulation = new BattleSimulation(4, 2, rules, seed);
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 100, 5, 2, 3, 37);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 100, 3, 2, 3, 100);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, attackerStats,
                HexDirection.East, out HexCharacter attacker), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0), targetStats,
                HexDirection.West, out HexCharacter target), Is.True);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Random expectedRolls = new Random(seed);

            for (int i = 0; i < 4; i++)
            {
                bool expectedCritical = expectedRolls.Next(100) < 37;
                BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);
                Assert.That(result.IsCritical, Is.EqualTo(expectedCritical));
                Assert.That(result.Damage, Is.EqualTo(expectedCritical ? 7 : 5));
            }
        }

        [Test]
        public void CancelledRejectedRemovedAndDuplicateCompletionsDoNotConsumeDamageRolls()
        {
            const int seed = 7;
            BattleSimulation simulation = new BattleSimulation(4, 1, new BattleRules(startingActionPoints: 8), seed);
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 100, 5, 2, 3, 50);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 100, 3, 2, 3, 0);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, attackerStats,
                HexDirection.East, out HexCharacter attacker), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0), targetStats,
                HexDirection.West, out HexCharacter first), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 0), targetStats,
                HexDirection.West, out HexCharacter second), Is.True);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Random expectedRolls = new Random(seed);

            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, attacker.Id), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, first.Id), Is.True);
            simulation.CancelAction();
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(attacker.ActionPoints, Is.EqualTo(8));

            Assert.That(simulation.TryAttack(attacker.Id, first.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            simulation.CancelAction();
            simulation.CancelAction();
            Assert.That(first.Health, Is.EqualTo(100));
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult firstResult), Is.True);
            bool firstCritical = expectedRolls.Next(100) < 50;
            Assert.That(firstResult.IsCritical, Is.EqualTo(firstCritical));
            Assert.That(first.Health, Is.EqualTo(firstCritical ? 93 : 95));
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);

            Assert.That(simulation.TryAttack(attacker.Id, first.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.RemoveUnit(first.Id), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);

            BattleAttackResult secondResult = CompleteAttack(simulation, attacker.Id, second.Id);
            bool secondCritical = expectedRolls.Next(100) < 50;
            Assert.That(secondResult.IsCritical, Is.EqualTo(secondCritical));
            Assert.That(second.Health, Is.EqualTo(secondCritical ? 93 : 95));
            Assert.That(attacker.ActionPoints, Is.EqualTo(2));
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
        }

        [Test]
        public void DamageRollsDoNotChangeFutureRoundOrder()
        {
            BattleSimulation attacking = CreateOrderBattle(out HexCharacter attacker, out HexCharacter target);
            BattleSimulation idle = CreateOrderBattle(out _, out _);
            Assert.That(attacking.TryStartBattle(), Is.True);
            Assert.That(idle.TryStartBattle(), Is.True);

            for (int i = 0; i < 20; i++)
            {
                Assert.That(attacking.CurrentUnitId, Is.EqualTo(idle.CurrentUnitId));
                Assert.That(attacking.RoundNumber, Is.EqualTo(idle.RoundNumber));
                int unitId = attacking.CurrentUnitId.Value;

                if (unitId == attacker.Id)
                {
                    CompleteAttack(attacking, attacker.Id, target.Id);
                }

                Assert.That(attacking.TryEndTurn(unitId), Is.True);
                Assert.That(idle.TryEndTurn(unitId), Is.True);
            }
        }

        static BattleSimulation CreateBattle(BattleTestUnit attackerStats, BattleTestUnit targetStats,
            out HexCharacter attacker, out HexCharacter target)
        {
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: 7);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, attackerStats,
                HexDirection.East, out attacker), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0), targetStats,
                HexDirection.West, out target), Is.True);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            return simulation;
        }

        static BattleSimulation CreateOrderBattle(out HexCharacter attacker, out HexCharacter target)
        {
            BattleSimulation simulation = new BattleSimulation(4, 1, randomSeed: 31);
            BattleTestUnit stats = new BattleTestUnit(MercenaryClass.Archer, 1000, 5, 2, 3, 50);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East, out attacker), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0), stats,
                HexDirection.West, out target), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 0), stats,
                HexDirection.East, out _), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0), stats,
                HexDirection.West, out _), Is.True);
            return simulation;
        }

        static BattleAttackResult CompleteAttack(BattleSimulation simulation, int attackerId, int targetId)
        {
            Assert.That(simulation.TryAttack(attackerId, targetId), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            return result;
        }
    }
}
