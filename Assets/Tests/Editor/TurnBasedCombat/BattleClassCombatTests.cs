using System;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleClassCombatTests
    {
        static readonly TraitCollection _negationTraits = new TraitCollection(new[]
        {
            new Trait(TraitType.DirectionalPhysicalNegation, new TraitSource(TraitSourceKind.Specialization, 0))
        });
        static readonly TraitCollection _rearCriticalTraits = new TraitCollection(new[]
        {
            new Trait(TraitType.RearGuaranteedCritical, new TraitSource(TraitSourceKind.Class, (int)MercenaryClass.Rogue))
        });
        static readonly BattleTraitSettings _negationSettings = new BattleTraitSettings(0.5f, 0.1f, 0f);

        [TestCase(1, 0, 49, true)]
        [TestCase(1, 0, 50, false)]
        [TestCase(1, 1, 49, true)]
        [TestCase(1, 1, 50, false)]
        [TestCase(2, -1, 49, true)]
        [TestCase(2, -1, 50, false)]
        [TestCase(1, -1, 9, true)]
        [TestCase(1, -1, 10, false)]
        [TestCase(0, 1, 9, true)]
        [TestCase(0, 1, 10, false)]
        [TestCase(0, -1, 9, true)]
        [TestCase(0, -1, 10, false)]
        [TestCase(-1, 1, 9, true)]
        [TestCase(-1, 1, 10, false)]
        [TestCase(1, -2, 9, true)]
        [TestCase(1, -2, 10, false)]
        [TestCase(-1, 2, 9, true)]
        [TestCase(-1, 0, 0, false)]
        [TestCase(-1, -1, 0, false)]
        [TestCase(-2, 1, 0, false)]
        public void DirectionalNegationTraitUsesFrontSideRearProbabilityBoundaries(
            int attackerQ, int attackerR, int roll, bool expectedNegation)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 100, 5, 2, 3, 0);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Warrior, 100, 3, 2, 1, 0,
                physicalDefense: 1);
            BattleSimulation simulation = CreateClassBattle(attackerStats, targetStats,
                new HexCoordinate(attackerQ, attackerR), FindSeed(roll), out HexCharacter attacker, out HexCharacter target,
                targetTraits: _negationTraits);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.IsCritical, Is.False);
            Assert.That(result.WasNegated, Is.EqualTo(expectedNegation));
            Assert.That(result.Damage, Is.EqualTo(expectedNegation ? 0 : 4));
        }

        [TestCase(AttackAttribute.Water)]
        [TestCase(AttackAttribute.Fire)]
        [TestCase(AttackAttribute.Electricity)]
        [TestCase(AttackAttribute.Poison)]
        [TestCase(AttackAttribute.Untyped)]
        public void DirectionalNegationTraitCannotNegateNonphysicalDamage(AttackAttribute attribute)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 100, 5, 2, 3, 0, attribute);
            BattleTestUnit targetStats = BattleTestUnit.ForClass(MercenaryClass.Warrior);
            BattleSimulation simulation = CreateClassBattle(attackerStats, targetStats,
                new HexCoordinate(1, 0), FindSeed(0), out HexCharacter attacker, out HexCharacter target,
                targetTraits: _negationTraits);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.AttackAttribute, Is.EqualTo(attribute));
            Assert.That(result.WasNegated, Is.False);
            Assert.That(result.Damage, Is.EqualTo(5));
        }

        [TestCase(0)]
        [TestCase(100)]
        public void DirectionalNegationTraitNegatesCriticalDamageAndCriticalEndpointsDoNotConsumeRandomRolls(int criticalChance)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 100, 5, 2, 3, criticalChance);
            BattleSimulation simulation = CreateClassBattle(attackerStats,
                BattleTestUnit.ForClass(MercenaryClass.Warrior), new HexCoordinate(1, 0), FindThresholdSeed(true, false),
                out HexCharacter attacker, out HexCharacter target, targetTraits: _negationTraits);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.IsCritical, Is.EqualTo(criticalChance == 100));
            Assert.That(result.WasNegated, Is.True);
            Assert.That(result.Damage, Is.Zero);
            Assert.That(target.Health, Is.EqualTo(10));
        }

        [TestCase(true, false, 6)]
        [TestCase(false, true, 0)]
        public void CriticalRollPrecedesDirectionalNegationRoll(
            bool expectedCritical, bool expectedNegation, int expectedDamage)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 100, 5, 2, 3, 50);
            BattleSimulation simulation = CreateClassBattle(attackerStats,
                BattleTestUnit.ForClass(MercenaryClass.Warrior), new HexCoordinate(1, 0),
                FindThresholdSeed(expectedCritical, expectedNegation),
                out HexCharacter attacker, out HexCharacter target, targetTraits: _negationTraits);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.IsCritical, Is.EqualTo(expectedCritical));
            Assert.That(result.WasNegated, Is.EqualTo(expectedNegation));
            Assert.That(result.Damage, Is.EqualTo(expectedDamage));
        }

        [Test]
        public void DirectionalNegationTraitStillRollsWhenDefenseAlreadyReducesDamageToZero()
        {
            BattleTestUnit stats = new BattleTestUnit(MercenaryClass.Archer, 100, 5, 2, 3, 0);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Warrior, 100, 3, 2, 1, 0,
                physicalDefense: 100);
            BattleSimulation simulation = CreateClassBattle(stats, targetStats, new HexCoordinate(1, 0),
                FindThresholdSeed(true, false), out HexCharacter attacker, out HexCharacter target,
                targetTraits: _negationTraits);

            BattleAttackResult first = CompleteAttack(simulation, attacker.Id, target.Id);
            BattleAttackResult second = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(first.Damage, Is.Zero);
            Assert.That(first.WasNegated, Is.True);
            Assert.That(second.Damage, Is.Zero);
            Assert.That(second.WasNegated, Is.False);
            Assert.That(target.Health, Is.EqualTo(100));
            Assert.That(attacker.ActionPoints, Is.Zero);
        }

        [TestCase(-1, 0, AttackAttribute.Physical, 0)]
        [TestCase(-1, -1, AttackAttribute.Physical, 0)]
        [TestCase(-2, 1, AttackAttribute.Physical, 0)]
        [TestCase(-2, 0, AttackAttribute.Physical, 0)]
        [TestCase(-3, 1, AttackAttribute.Physical, 0)]
        [TestCase(-1, -2, AttackAttribute.Physical, 0)]
        [TestCase(-3, 2, AttackAttribute.Physical, 0)]
        [TestCase(-2, 0, AttackAttribute.Untyped, 0)]
        [TestCase(-2, 0, AttackAttribute.Untyped, 100)]
        [TestCase(-1, 0, AttackAttribute.Physical, 100)]
        public void RearCriticalTraitForcesExactlyOneCriticalIncludingRangedAndUntypedAttacks(
            int attackerQ, int attackerR, AttackAttribute attribute, int criticalChance)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Rogue, 100, 5, 4, 3,
                criticalChance, attribute);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 100, 3, 2, 3, 0);
            BattleSimulation simulation = CreateClassBattle(attackerStats, targetStats,
                new HexCoordinate(attackerQ, attackerR), 7, out HexCharacter attacker, out HexCharacter target,
                attackerTraits: _rearCriticalTraits);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.IsCritical, Is.True);
            Assert.That(result.Damage, Is.EqualTo(7));
            Assert.That(result.AttackAttribute, Is.EqualTo(attribute));
            Assert.That(attacker.MovementRemaining, Is.EqualTo(4));
        }

        [TestCase(1, 0)]
        [TestCase(1, -1)]
        [TestCase(0, 1)]
        [TestCase(1, -2)]
        [TestCase(-1, 2)]
        [TestCase(0, -1)]
        [TestCase(-1, 1)]
        [TestCase(0, -3)]
        [TestCase(-3, 3)]
        public void RearCriticalTraitUsesOrdinaryChanceFromFrontAndSide(int attackerQ, int attackerR)
        {
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Rogue, 100, 5, 4, 3, 0);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 100, 3, 2, 3, 0);
            BattleSimulation simulation = CreateClassBattle(attackerStats, targetStats,
                new HexCoordinate(attackerQ, attackerR), 7, out HexCharacter attacker, out HexCharacter target,
                attackerTraits: _rearCriticalTraits);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.IsCritical, Is.False);
            Assert.That(result.Damage, Is.EqualTo(5));
        }

        [Test]
        public void ForcedRearTraitCriticalDoesNotConsumeOrdinaryChanceRoll()
        {
            int seed = FindThresholdSeed(true, false);
            BattleSimulation simulation = new BattleSimulation(5, 3, randomSeed: seed);
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Rogue, 100, 5, 4, 3, 50);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 100, 3, 2, 3, 0);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1), attackerStats,
                HexDirection.East, out HexCharacter attacker, _rearCriticalTraits), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 1), targetStats,
                HexDirection.East, out HexCharacter rearTarget), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1), targetStats,
                HexDirection.East, out HexCharacter frontTarget), Is.True);
            BattleTestUtility.StartTurn(simulation, attacker.Id);

            BattleAttackResult forced = CompleteAttack(simulation, attacker.Id, rearTarget.Id);
            BattleAttackResult rolled = CompleteAttack(simulation, attacker.Id, frontTarget.Id);

            Assert.That(forced.IsCritical, Is.True);
            Assert.That(forced.Damage, Is.EqualTo(7));
            Assert.That(rolled.IsCritical, Is.True);
            Assert.That(rolled.Damage, Is.EqualTo(7));
        }

        [Test]
        public void RogueApproachUsesTheCenterLineAndResolvesTraitsAtTheChosenLanding()
        {
            BattleSimulation simulation = new BattleSimulation(6, 5, randomSeed: 7);
            BattleTestUnit rogueStats = new BattleTestUnit(MercenaryClass.Rogue, 100, 3, 4, 1, 0);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 100, 3, 2, 3, 0);
            HexCoordinate start = new HexCoordinate(2, 2);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, start, rogueStats,
                HexDirection.West, out HexCharacter rogue, _rearCriticalTraits), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 3), targetStats,
                HexDirection.East, out HexCharacter target), Is.True);
            HexCoordinate rearAlternative = new HexCoordinate(3, 3);
            Assert.That(start.DistanceTo(rearAlternative), Is.EqualTo(2));
            Assert.That(rearAlternative.DistanceTo(target.Coordinate), Is.EqualTo(1));
            Assert.That(BattleFacing.Classify(target.Facing, target.Coordinate, rearAlternative),
                Is.EqualTo(BattleRelativeDirection.Rear));
            BattleTestUtility.StartTurn(simulation, rogue.Id);

            Assert.That(simulation.TryAttack(rogue.Id, target.Id), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep first), Is.True);
            Assert.That(first.To, Is.EqualTo(new HexCoordinate(3, 2)));
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep second), Is.True);
            Assert.That(second.To, Is.EqualTo(new HexCoordinate(3, 3)));
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(BattleFacing.Classify(target.Facing, target.Coordinate, rogue.Coordinate),
                Is.EqualTo(BattleRelativeDirection.Rear));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.IsCritical, Is.True);
            Assert.That(result.Damage, Is.EqualTo(5));
            Assert.That(rogue.MovementRemaining, Is.EqualTo(2));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ArcherAttacksAtEveryDistanceWithinThreeWithoutMoving(int distance)
        {
            BattleSimulation simulation = CreateArcherBattle(BattleTeam.Player, distance,
                out HexCharacter archer, out HexCharacter target);

            Assert.That(simulation.TryAttack(archer.Id, target.Id), Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(archer.MovementRemaining, Is.EqualTo(2));
            Assert.That(archer.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(archer.ActionPoints, Is.EqualTo(2));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(archer.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(archer.MovementRemaining, Is.EqualTo(2));
        }

        [Test]
        public void ArcherAtDistanceFourApproachesOneTileBeforeIssuingAttack()
        {
            BattleSimulation simulation = CreateArcherBattle(BattleTeam.Player, 4,
                out HexCharacter archer, out HexCharacter target);

            Assert.That(simulation.TryAttack(archer.Id, target.Id), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(archer.ActionPoints, Is.EqualTo(4));
            Assert.That(archer.MovementRemaining, Is.EqualTo(1));
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
        }

        [Test]
        public void ArcherRejectsEntireApproachWhenItExceedsMovementBudget()
        {
            BattleSimulation simulation = CreateArcherBattle(BattleTeam.Player, 6,
                out HexCharacter archer, out HexCharacter target);

            Assert.That(simulation.TryAttack(archer.Id, target.Id), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(archer.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(archer.ActionPoints, Is.EqualTo(4));
            Assert.That(archer.MovementRemaining, Is.EqualTo(2));
        }

        [Test]
        public void OccupiedIntermediateTilesDoNotBlockRangedAttack()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1, randomSeed: 7);
            BattleTestUnit stats = BattleTestUnit.ForClass(MercenaryClass.Archer);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East,
                out HexCharacter archer), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0), stats,
                HexDirection.West, out HexCharacter enemyBlocker), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 0), stats,
                HexDirection.East, out HexCharacter allyBlocker), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0), stats,
                HexDirection.West, out HexCharacter target), Is.True);
            BattleTestUtility.StartTurn(simulation, archer.Id);

            CompleteAttack(simulation, archer.Id, target.Id);

            Assert.That(archer.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(archer.MovementRemaining, Is.EqualTo(2));
            Assert.That(target.Health, Is.LessThan(8));
            Assert.That(enemyBlocker.Health, Is.EqualTo(8));
            Assert.That(allyBlocker.Health, Is.EqualTo(8));
        }

        [TestCase(1, BattleAiDecisionKind.Attack, 0)]
        [TestCase(2, BattleAiDecisionKind.Attack, 0)]
        [TestCase(3, BattleAiDecisionKind.Attack, 0)]
        [TestCase(4, BattleAiDecisionKind.Attack, 1)]
        [TestCase(6, BattleAiDecisionKind.Move, 2)]
        public void EnemyArcherUsesOwnRangeAndPreservesPartialApproachPolicy(
            int distance, BattleAiDecisionKind expectedKind, int destinationQ)
        {
            BattleSimulation simulation = CreateArcherBattle(BattleTeam.Enemy, distance,
                out HexCharacter archer, out HexCharacter target);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(expectedKind));
            Assert.That(decision.UnitId, Is.EqualTo(archer.Id));
            Assert.That(decision.TargetId, Is.EqualTo(target.Id));
            Assert.That(archer.ActionPoints, Is.EqualTo(4));
            Assert.That(archer.MovementRemaining, Is.EqualTo(2));

            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(destinationQ, 0)));
        }

        static BattleSimulation CreateClassBattle(BattleTestUnit attackerStats, BattleTestUnit targetStats,
            HexCoordinate attackOffset, int seed, out HexCharacter attacker, out HexCharacter target,
            TraitCollection attackerTraits = null, TraitCollection targetTraits = null)
        {
            BattleSimulation simulation = new BattleSimulation(7, 7, randomSeed: seed);
            HexCoordinate targetCoordinate = new HexCoordinate(3, 3);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, targetCoordinate + attackOffset, attackerStats,
                HexDirection.West, out attacker, attackerTraits), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, targetCoordinate, targetStats,
                HexDirection.East, out target, targetTraits, _negationSettings), Is.True);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            return simulation;
        }

        static BattleSimulation CreateArcherBattle(BattleTeam archerTeam, int distance,
            out HexCharacter archer, out HexCharacter target)
        {
            BattleSimulation simulation = new BattleSimulation(7, 1, randomSeed: 7);
            Assert.That(simulation.TrySpawnUnit(archerTeam, default, BattleTestUnit.ForClass(MercenaryClass.Archer),
                HexDirection.East, out archer), Is.True);
            BattleTeam targetTeam = archerTeam == BattleTeam.Player ? BattleTeam.Enemy : BattleTeam.Player;
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 100, 3, 2, 3, 0);
            Assert.That(simulation.TrySpawnUnit(targetTeam, new HexCoordinate(distance, 0), targetStats,
                HexDirection.West, out target), Is.True);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            return simulation;
        }

        static BattleAttackResult CompleteAttack(BattleSimulation simulation, int attackerId, int targetId)
        {
            Assert.That(simulation.TryAttack(attackerId, targetId), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            return result;
        }

        static int FindSeed(int firstRoll)
        {
            for (int seed = 0; seed < 100000; seed++)
            {
                Random random = new Random(seed);

                if (random.Next(100) == firstRoll)
                {
                    return seed;
                }
            }

            Assert.Fail("No deterministic seed found for the requested roll boundary.");
            return 0;
        }

        static int FindThresholdSeed(bool firstBelowThreshold, bool secondBelowThreshold)
        {
            for (int seed = 0; seed < 100000; seed++)
            {
                Random random = new Random(seed);
                int firstRoll = random.Next(100);
                int secondRoll = random.Next(100);

                if ((firstRoll < 50) == firstBelowThreshold && (secondRoll < 50) == secondBelowThreshold)
                {
                    return seed;
                }
            }

            Assert.Fail("No deterministic seed found for the requested roll thresholds.");
            return 0;
        }
    }
}
