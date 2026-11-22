using System;
using RaggedBanner.HexGrid;
using NUnit.Framework;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleEnemyAITests
    {
        [Test]
        public void AdjacentTargetUsesLowestIdAndDecisionDoesNotSpendResources()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 1);
            HexCharacter first = Spawn(simulation, BattleTeam.Player, 2, 1);
            Spawn(simulation, BattleTeam.Player, 1, 2);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            for (int i = 0; i < 3; i++)
            {
                BattleAiDecision decision = ai.Decide(simulation);
                Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
                Assert.That(decision.UnitId, Is.EqualTo(enemy.Id));
                Assert.That(decision.TargetId, Is.EqualTo(first.Id));
                Assert.That(decision.Destination, Is.EqualTo(enemy.Coordinate));
                Assert.That(enemy.ActionPoints, Is.EqualTo(4));
                Assert.That(enemy.MovementRemaining, Is.EqualTo(2));
                Assert.That(first.Health, Is.EqualTo(10));
                Assert.That(simulation.IsBusy, Is.False);
            }
        }

        [Test]
        public void UnreachableCloserTargetDoesNotHideReachableTarget()
        {
            BattleSimulation simulation = new BattleSimulation(6, 3, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 2, 0,
                BattleTestUnit.ForClass(MercenaryClass.Warrior));
            Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter reachable = Spawn(simulation, BattleTeam.Player, 5, 0);
            Spawn(simulation, BattleTeam.Enemy, 1, 0);
            Spawn(simulation, BattleTeam.Enemy, 0, 1);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Move));
            Assert.That(decision.TargetId, Is.EqualTo(reachable.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(4, 0)));
            Assert.That(simulation.TryMove(enemy.Id, decision.Destination), Is.True);
        }

        [Test]
        public void TargetSelectionUsesTraversablePathLengthInsteadOfHexDistance()
        {
            BattleSimulation simulation = new BattleSimulation(6, 6, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0,
                new BattleTestUnit(MercenaryClass.Warrior, 10, 3, 20, 1, 0));
            HexCharacter near = Spawn(simulation, BattleTeam.Player, 3, 0);
            HexCharacter faster = Spawn(simulation, BattleTeam.Player, 0, 5);

            for (int r = 0; r < 4; r++)
            {
                Spawn(simulation, BattleTeam.Enemy, 1, r);
            }

            StartOn(simulation, enemy.Id);
            Assert.That(enemy.Coordinate.DistanceTo(near.Coordinate), Is.LessThan(enemy.Coordinate.DistanceTo(faster.Coordinate)));

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Move));
            Assert.That(decision.TargetId, Is.EqualTo(faster.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(0, 4)));
        }

        [Test]
        public void EqualApproachDistancesUseLowestTargetIdAndStablePath()
        {
            BattleSimulation simulation = new BattleSimulation(5, 5, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 2, 2,
                BattleTestUnit.ForClass(MercenaryClass.Warrior));
            HexCharacter first = Spawn(simulation, BattleTeam.Player, 4, 2);
            Spawn(simulation, BattleTeam.Player, 2, 4);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            for (int i = 0; i < 3; i++)
            {
                BattleAiDecision decision = ai.Decide(simulation);
                Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Move));
                Assert.That(decision.TargetId, Is.EqualTo(first.Id));
                Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(3, 2)));
                Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(2, 2)));
            }
        }

        [Test]
        public void InsufficientApStillAllowsPartialApproachThenEndsTurn()
        {
            BattleRules rules = new BattleRules(startingActionPoints: 0, actionPointsPerTurn: 0);
            BattleSimulation simulation = new BattleSimulation(7, 1, rules, 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0);
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 6, 0);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            BattleAiDecision decision = ai.Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Move));
            Assert.That(decision.TargetId, Is.EqualTo(target.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(simulation.TryAttack(enemy.Id, target.Id), Is.False);
            Assert.That(simulation.TryMove(enemy.Id, decision.Destination), Is.True);
            CompleteMovement(simulation, 2);
            Assert.That(enemy.MovementRemaining, Is.Zero);
            Assert.That(enemy.ActionPoints, Is.Zero);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(enemy.Id));
        }

        [Test]
        public void AdjacentTargetWithoutApEndsTurnEvenWhenMovementRemains()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2, new BattleRules(startingActionPoints: 1), 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0);
            Spawn(simulation, BattleTeam.Player, 1, 0);
            Spawn(simulation, BattleTeam.Player, 3, 1);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
            Assert.That(decision.UnitId, Is.EqualTo(enemy.Id));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(2));
        }

        [Test]
        public void NoReachableTargetEndsTurnWithoutChangingState()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0);
            Spawn(simulation, BattleTeam.Enemy, 1, 0);
            Spawn(simulation, BattleTeam.Player, 3, 0);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(enemy.Id));
            Assert.That(enemy.ActionPoints, Is.EqualTo(4));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(2));
        }

        [Test]
        public void NoMovementAndNoAdjacentTargetEndsTurn()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0,
                BattleTestUtility.CreateStats(movementPerTurn: 0));
            Spawn(simulation, BattleTeam.Player, 3, 0);
            StartOn(simulation, enemy.Id);

            Assert.That(new BattleEnemyAI().Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
        }

        [Test]
        public void DecisionWaitsThroughApproachAndAttackThenRepeatsUntilApIsSpent()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0);
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 3, 0);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();
            BattleAiDecision approachAttack = ai.Decide(simulation);
            Assert.That(approachAttack.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(approachAttack.Destination, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(simulation.TryAttack(enemy.Id, approachAttack.TargetId, approachAttack.Destination), Is.True);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.None));
            CompleteMovement(simulation, 2);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.None));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.None));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);

            BattleAiDecision attack = ai.Decide(simulation);
            Assert.That(attack.TargetId, Is.EqualTo(target.Id));
            Assert.That(attack.Destination, Is.EqualTo(enemy.Coordinate));
            CompleteAttack(simulation, attack);

            Assert.That(target.Health, Is.EqualTo(4));
            Assert.That(enemy.ActionPoints, Is.Zero);
            Assert.That(enemy.MovementRemaining, Is.Zero);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
        }

        [Test]
        public void DeadTargetIsReplacedAndLastDeathStopsDecisions()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 1);
            HexCharacter first = Spawn(simulation, BattleTeam.Player, 2, 1,
                BattleTestUtility.CreateStats(maxHealth: 3));
            HexCharacter second = Spawn(simulation, BattleTeam.Player, 1, 2,
                BattleTestUtility.CreateStats(maxHealth: 3));
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            BattleAiDecision firstAttack = ai.Decide(simulation);
            Assert.That(firstAttack.TargetId, Is.EqualTo(first.Id));
            CompleteAttack(simulation, firstAttack);
            Assert.That(simulation.Board.TryGetUnit(first.Id, out _), Is.False);
            Assert.That(simulation.Board.IsOccupied(first.Coordinate), Is.False);
            BattleAiDecision secondAttack = ai.Decide(simulation);
            Assert.That(secondAttack.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(secondAttack.TargetId, Is.EqualTo(second.Id));
            CompleteAttack(simulation, secondAttack);

            Assert.That(enemy.ActionPoints, Is.Zero);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.None));
        }

        [Test]
        public void NullPreparingWaitingAndPlayerTurnDoNotProduceDecisions()
        {
            BattleEnemyAI ai = new BattleEnemyAI();
            Assert.That(ai.Decide(null).Kind, Is.EqualTo(BattleAiDecisionKind.None));
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: 11);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0);
            Spawn(simulation, BattleTeam.Enemy, 2, 0);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.None));
            StartOn(simulation, player.Id);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.None));

            BattleSimulation singleTeam = new BattleSimulation(2, 1);
            Spawn(singleTeam, BattleTeam.Enemy, 0, 0);
            singleTeam.TryStartBattle();
            Assert.That(singleTeam.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(ai.Decide(singleTeam).Kind, Is.EqualTo(BattleAiDecisionKind.None));
            BattleSimulation empty = new BattleSimulation(1, 1);
            empty.TryStartBattle();
            Assert.That(empty.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(ai.Decide(empty).Kind, Is.EqualTo(BattleAiDecisionKind.None));
        }

        [Test]
        public void ArcherAttacksLowestIdInRangeAcrossOccupiedHexes()
        {
            BattleSimulation simulation = new BattleSimulation(5, 2, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0,
                BattleTestUtility.CreateStats(attackRange: 3));
            HexCharacter first = Spawn(simulation, BattleTeam.Player, 3, 0);
            Spawn(simulation, BattleTeam.Player, 0, 1);
            Spawn(simulation, BattleTeam.Enemy, 1, 0);
            Spawn(simulation, BattleTeam.Enemy, 2, 0);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(first.Id));
            CompleteAttack(simulation, decision);
            Assert.That(enemy.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(2));
            Assert.That(first.Health, Is.EqualTo(7));
        }

        [TestCase(4, 1, BattleAiDecisionKind.Attack)]
        [TestCase(7, 2, BattleAiDecisionKind.Move)]
        public void ArcherApproachUsesOwnRangeAndMovementBudget(int targetQ, int expectedSteps,
            BattleAiDecisionKind expectedDecision)
        {
            BattleSimulation simulation = new BattleSimulation(8, 1, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0,
                BattleTestUtility.CreateStats(attackRange: 3));
            HexCharacter target = Spawn(simulation, BattleTeam.Player, targetQ, 0);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            BattleAiDecision decision = ai.Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(expectedDecision));
            Assert.That(decision.TargetId, Is.EqualTo(target.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(expectedSteps, 0)));

            if (decision.Kind == BattleAiDecisionKind.Attack)
            {
                Assert.That(simulation.TryAttack(enemy.Id, decision.TargetId, decision.Destination), Is.True);
            }
            else
            {
                Assert.That(simulation.TryMove(enemy.Id, decision.Destination), Is.True);
            }

            CompleteMovement(simulation, expectedSteps);
            Assert.That(enemy.ActionPoints, Is.EqualTo(4));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(2 - expectedSteps));

            if (decision.Kind == BattleAiDecisionKind.Attack)
            {
                Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.None));
                Assert.That(simulation.TryBeginAttack(out _), Is.True);
                Assert.That(simulation.TryCompleteAttack(out _), Is.True);
                Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            }
            else
            {
                Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
            }
        }

        [Test]
        public void RogueMovesBehindAdjacentTargetThenAttacksFromTheSelectedPosition()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 2,
                BattleTestUnit.ForClass(MercenaryClass.Rogue), new TraitCollection(new[]
                {
                    new Trait(TraitType.RearGuaranteedCritical,
                        new TraitSource(TraitSourceKind.Class, (int)MercenaryClass.Rogue))
                }));
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 2, 1);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            BattleAiDecision decision = ai.Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(target.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(1, 1)));
            Assert.That(simulation.TryAttack(enemy.Id, decision.TargetId, decision.Destination), Is.True);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.None));
            CompleteMovement(simulation, 1);
            Assert.That(enemy.Coordinate, Is.EqualTo(decision.Destination));
            Assert.That(enemy.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.IsCritical, Is.True);
            Assert.That(target.Health, Is.EqualTo(5));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(3));
            Assert.That(enemy.ActionPoints, Is.EqualTo(2));
            BattleAiDecision next = ai.Decide(simulation);
            Assert.That(next.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(next.TargetId, Is.EqualTo(target.Id));
            Assert.That(next.Destination, Is.EqualTo(enemy.Coordinate));
        }

        [Test]
        public void RogueCurrentRearBeatsLowerIdTargetThatRequiresMovement()
        {
            BattleSimulation simulation = new BattleSimulation(5, 4, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 1,
                BattleTestUnit.ForClass(MercenaryClass.Rogue));
            Spawn(simulation, BattleTeam.Player, 2, 2);
            HexCharacter rearTarget = Spawn(simulation, BattleTeam.Player, 2, 1);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(rearTarget.Id));
            Assert.That(decision.Destination, Is.EqualTo(enemy.Coordinate));
        }

        [Test]
        public void RogueRearSelectionUsesActualPathLengthInsteadOfHexDistance()
        {
            BattleSimulation simulation = new BattleSimulation(6, 6, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0,
                new BattleTestUnit(MercenaryClass.Rogue, 10, 3, 20, 1, 0));
            HexCharacter near = Spawn(simulation, BattleTeam.Player, 3, 0);
            HexCharacter faster = Spawn(simulation, BattleTeam.Player, 1, 4);

            for (int r = 0; r < 4; r++)
            {
                Spawn(simulation, BattleTeam.Enemy, 1, r);
            }

            StartOn(simulation, enemy.Id);
            Assert.That(enemy.Coordinate.DistanceTo(near.Coordinate.GetNeighbor(HexDirection.West)),
                Is.LessThan(enemy.Coordinate.DistanceTo(faster.Coordinate.GetNeighbor(HexDirection.West))));

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(faster.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(0, 4)));
            Assert.That(simulation.TryAttack(enemy.Id, decision.TargetId, decision.Destination), Is.True);
            CompleteMovement(simulation, 4);
            Assert.That(enemy.Coordinate, Is.EqualTo(decision.Destination));
        }

        [Test]
        public void RogueEqualRearPathsUseLowestTargetIdBeforeExplorationOrder()
        {
            BattleSimulation simulation = new BattleSimulation(6, 5, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 2, 2,
                BattleTestUnit.ForClass(MercenaryClass.Rogue));
            HexCharacter first = Spawn(simulation, BattleTeam.Player, 2, 3);
            Spawn(simulation, BattleTeam.Player, 4, 2);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(first.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(1, 3)));
        }

        [TestCase(2, 4, 1, 4)]
        [TestCase(3, 2, 2, 2)]
        public void RogueLongerRangeIncludesBothShiftedRearSectorBoundaries(
            int startQ, int startR, int expectedQ, int expectedR)
        {
            BattleSimulation simulation = new BattleSimulation(6, 6, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, startQ, startR,
                new BattleTestUnit(MercenaryClass.Rogue, 10, 3, 1, 2, 0));
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 3, 3);
            Spawn(simulation, BattleTeam.Enemy, 2, 3);
            StartOn(simulation, enemy.Id);
            Assert.That(BattleFacing.Classify(target.Facing, target.Coordinate, enemy.Coordinate),
                Is.EqualTo(BattleRelativeDirection.Side));

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(target.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(expectedQ, expectedR)));
            Assert.That(decision.Destination.DistanceTo(target.Coordinate), Is.EqualTo(2));
            Assert.That(BattleFacing.Classify(target.Facing, target.Coordinate, decision.Destination),
                Is.EqualTo(BattleRelativeDirection.Rear));
        }

        [TestCase(0, false)]
        [TestCase(2, false)]
        [TestCase(4, true)]
        public void RogueUsesCurrentGeneralAttackWhenRearIsOverBudgetOrOccupied(int movement, bool occupyRear)
        {
            BattleSimulation simulation = new BattleSimulation(5, 3, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 3, 1,
                new BattleTestUnit(MercenaryClass.Rogue, 10, 3, movement, 1, 0));
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 2, 1);

            if (occupyRear)
            {
                Spawn(simulation, BattleTeam.Enemy, 1, 1);
            }

            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(target.Id));
            Assert.That(decision.Destination, Is.EqualTo(enemy.Coordinate));
            CompleteAttack(simulation, decision);
            Assert.That(enemy.MovementRemaining, Is.EqualTo(movement));
            Assert.That(target.Health, Is.EqualTo(7));
        }

        [Test]
        public void RogueDoesNotCrossAnOccupiedTargetToReachItsRear()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 3, 0,
                BattleTestUnit.ForClass(MercenaryClass.Rogue));
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 2, 0);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(target.Id));
            Assert.That(decision.Destination, Is.EqualTo(enemy.Coordinate));
        }

        [Test]
        public void RogueWithoutAttackApDoesNotMoveToReachableRear()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, new BattleRules(startingActionPoints: 1), 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 2,
                BattleTestUnit.ForClass(MercenaryClass.Rogue));
            Spawn(simulation, BattleTeam.Player, 2, 1);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(1, 2)));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(4));
            Assert.That(enemy.ActionPoints, Is.EqualTo(1));
        }

        [TestCase(MercenaryClass.Rogue)]
        [TestCase(MercenaryClass.Archer)]
        public void InsufficientApApproachStopsAtActualRangeAndDoesNotSpendRemainingMovement(MercenaryClass unitClass)
        {
            BattleRules rules = new BattleRules(startingActionPoints: 0, actionPointsPerTurn: 0);
            BattleSimulation simulation = new BattleSimulation(6, 1, rules, 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0,
                new BattleTestUnit(unitClass, 10, 3, 4, 3, 0));
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 4, 0);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            BattleAiDecision decision = ai.Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Move));
            Assert.That(decision.TargetId, Is.EqualTo(target.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(simulation.TryMove(enemy.Id, decision.Destination), Is.True);
            CompleteMovement(simulation, 1);
            Assert.That(enemy.MovementRemaining, Is.EqualTo(3));
            Assert.That(enemy.ActionPoints, Is.Zero);
            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
        }

        [Test]
        public void ArcherKeepsAttackingAdjacentTargetInsteadOfApproachingLowerIdTargetOrRetreating()
        {
            BattleSimulation simulation = new BattleSimulation(7, 3, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 2, 1,
                BattleTestUtility.CreateStats(movementPerTurn: 4, attackRange: 2));
            Spawn(simulation, BattleTeam.Player, 5, 1);
            HexCharacter adjacent = Spawn(simulation, BattleTeam.Player, 3, 1);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            for (int i = 0; i < 2; i++)
            {
                BattleAiDecision decision = ai.Decide(simulation);
                Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
                Assert.That(decision.TargetId, Is.EqualTo(adjacent.Id));
                Assert.That(decision.Destination, Is.EqualTo(enemy.Coordinate));
                CompleteAttack(simulation, decision);
                Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(2, 1)));
                Assert.That(enemy.MovementRemaining, Is.EqualTo(4));
            }

            Assert.That(ai.Decide(simulation).Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
        }

        [Test]
        public void ArcherSelectsMaximumTargetDistanceThenShortestStablePath()
        {
            BattleSimulation simulation = new BattleSimulation(7, 5, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0,
                BattleTestUtility.CreateStats(movementPerTurn: 4, attackRange: 3));
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 4, 1);
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();

            for (int i = 0; i < 3; i++)
            {
                BattleAiDecision decision = ai.Decide(simulation);
                Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
                Assert.That(decision.TargetId, Is.EqualTo(target.Id));
                Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(2, 0)));
                Assert.That(decision.Destination.DistanceTo(target.Coordinate), Is.EqualTo(enemy.Skills.BasicAttack.MaxRange));
                Assert.That(enemy.Coordinate, Is.EqualTo(default(HexCoordinate)));
            }
        }

        [Test]
        public void ArcherEqualRangeAndPathLengthUseLowestTargetIdBeforeExplorationOrder()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 2, 2,
                BattleTestUtility.CreateStats(attackRange: 3));
            HexCharacter first = Spawn(simulation, BattleTeam.Player, 2, 6);
            Spawn(simulation, BattleTeam.Player, 6, 2);
            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(first.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(2, 3)));
        }

        [Test]
        public void ArcherEqualRangePrefersShorterActualPathOverNearerLowerIdTarget()
        {
            BattleSimulation simulation = new BattleSimulation(7, 7, randomSeed: 11);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 0, 0,
                BattleTestUtility.CreateStats(movementPerTurn: 20, attackRange: 3));
            Spawn(simulation, BattleTeam.Player, 4, 0);
            HexCharacter faster = Spawn(simulation, BattleTeam.Player, 0, 5);

            for (int r = 0; r < 4; r++)
            {
                Spawn(simulation, BattleTeam.Enemy, 1, r);
            }

            StartOn(simulation, enemy.Id);

            BattleAiDecision decision = new BattleEnemyAI().Decide(simulation);

            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.TargetId, Is.EqualTo(faster.Id));
            Assert.That(decision.Destination, Is.EqualTo(new HexCoordinate(0, 2)));
        }

        [TestCase(MercenaryClass.Rogue)]
        [TestCase(MercenaryClass.Archer)]
        public void RepeatedDecisionsDoNotConsumeCriticalOrNegationRolls(MercenaryClass unitClass)
        {
            const int seed = 11;
            BattleRules rules = new BattleRules(startingActionPoints: 8);
            BattleSimulation simulation = new BattleSimulation(4, 3, rules, seed);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 2, 1,
                new BattleTestUnit(unitClass, 100, 3, 0, 1, 37));
            HexCharacter target = Spawn(simulation, BattleTeam.Player, 1, 1,
                new BattleTestUnit(MercenaryClass.Warrior, 100, 3, 0, 1, 0), new TraitCollection(new[]
                {
                    new Trait(TraitType.DirectionalPhysicalNegation,
                        new TraitSource(TraitSourceKind.Specialization, 1))
                }), new BattleTraitSettings(0.5f, 0.1f, 0f));
            StartOn(simulation, enemy.Id);
            BattleEnemyAI ai = new BattleEnemyAI();
            Random expectedRolls = new Random(seed);

            for (int attackIndex = 0; attackIndex < 4; attackIndex++)
            {
                BattleAiDecision decision = default;

                for (int evaluation = 0; evaluation < 5; evaluation++)
                {
                    decision = ai.Decide(simulation);
                    Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
                    Assert.That(decision.TargetId, Is.EqualTo(target.Id));
                    Assert.That(decision.Destination, Is.EqualTo(enemy.Coordinate));
                    Assert.That(enemy.ActionPoints, Is.EqualTo(8 - attackIndex * enemy.Skills.BasicAttack.ActionPointCost));
                    Assert.That(enemy.MovementRemaining, Is.Zero);
                    Assert.That(simulation.IsBusy, Is.False);
                }

                bool expectedCritical = expectedRolls.Next(100) < 37;
                bool expectedNegation = expectedRolls.NextDouble() < 0.5f;
                Assert.That(simulation.TryAttack(enemy.Id, decision.TargetId, decision.Destination), Is.True);
                Assert.That(simulation.TryBeginAttack(out _), Is.True);
                Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
                Assert.That(result.IsCritical, Is.EqualTo(expectedCritical));
                Assert.That(result.WasNegated, Is.EqualTo(expectedNegation));
            }
        }

        static HexCharacter Spawn(BattleSimulation simulation, BattleTeam team, int q, int r,
            BattleTestUnit stats = null, TraitCollection traits = null, BattleTraitSettings traitSettings = null)
        {
            Assert.That(simulation.TrySpawnUnit(team, new HexCoordinate(q, r),
                stats ?? BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter unit,
                traits, traitSettings), Is.True);
            return unit;
        }

        static void StartOn(BattleSimulation simulation, int unitId)
        {
            Assert.That(simulation.TryStartBattle(), Is.True);

            for (int i = 0; i < simulation.Board.Units.Count; i++)
            {
                if (simulation.CurrentUnitId == unitId)
                {
                    return;
                }

                Assert.That(simulation.CurrentUnitId.HasValue, Is.True);
                Assert.That(simulation.TryEndTurn(simulation.CurrentUnitId.Value), Is.True);
            }

            Assert.Fail("Requested unit did not receive its first turn.");
        }

        static void CompleteMovement(BattleSimulation simulation, int expectedSteps)
        {
            for (int i = 0; i < expectedSteps; i++)
            {
                Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
                Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(simulation.Phase, Is.EqualTo(simulation.TargetUnitId == 0
                ? BattleActionPhase.None : BattleActionPhase.ReadyToAttack));
        }

        static void CompleteAttack(BattleSimulation simulation, BattleAiDecision decision)
        {
            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(simulation.TryAttack(decision.UnitId, decision.TargetId, decision.Destination), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
        }
    }
}
