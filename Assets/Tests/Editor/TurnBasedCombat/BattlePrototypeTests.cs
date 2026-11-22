using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using NUnit.Framework;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattlePrototypeTests
    {
        [Test]
        public void PreparingHasNoTurnAndBattleCanOnlyStartOnce()
        {
            BattleSimulation simulation = CreateAdjacentBattle(out HexCharacter player, out HexCharacter enemy);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Preparing));
            Assert.That(simulation.RoundNumber, Is.Zero);
            Assert.That(simulation.CurrentUnitId, Is.Null);
            Assert.That(player.ActionPoints, Is.EqualTo(4));
            Assert.That(player.MovementRemaining, Is.Zero);
            Assert.That(simulation.TryAttack(player.Id, enemy.Id), Is.False);
            Assert.That(simulation.TryEndTurn(player.Id), Is.False);
            Assert.That(simulation.TryStartBattle(), Is.True);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Running));
            Assert.That(simulation.RoundNumber, Is.EqualTo(1));
            Assert.That(simulation.TryStartBattle(), Is.False);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EmptyOrSingleTeamBattleWaitsWithoutGeneratingRounds(int participantType)
        {
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: 7);
            HexCharacter unit = null;

            if (participantType != 0)
            {
                simulation.TrySpawnUnit(participantType == 1 ? BattleTeam.Player : BattleTeam.Enemy, default,
                    BattleTestUtility.CreateStats(), HexDirection.East, out unit);
            }

            Assert.That(simulation.TryStartBattle(), Is.True);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(simulation.RoundNumber, Is.Zero);
            Assert.That(simulation.CurrentUnitId, Is.Null);

            for (int i = 0; i < 3; i++)
            {
                Assert.That(simulation.TryEndTurn(unit == null ? 0 : unit.Id), Is.False);
                Assert.That(simulation.TryStartBattle(), Is.False);
            }

            Assert.That(simulation.RoundNumber, Is.Zero);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);

            if (unit != null)
            {
                Assert.That(unit.ActionPoints, Is.EqualTo(4));
                Assert.That(unit.MovementRemaining, Is.Zero);
                Assert.That(simulation.TryMove(unit.Id, new HexCoordinate(1, 0)), Is.False);
            }
        }

        [Test]
        public void PlayerLimitRejectsExtraRegistrationAndPreparationRemovalReleasesSlot()
        {
            BattleSimulation simulation = new BattleSimulation(5, 1, new BattleRules(maxPlayerUnits: 2), 7);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter first), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);
            Assert.That(simulation.Board.IsOccupied(new HexCoordinate(2, 0)), Is.False);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter enemy), Is.True);
            Assert.That(simulation.RemoveUnit(first.Id), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter replacement), Is.True);
            Assert.That(replacement.Id, Is.GreaterThan(enemy.Id));
            Assert.That(simulation.State, Is.EqualTo(BattleState.Preparing));
            Assert.That(simulation.TryStartBattle(), Is.True);
        }

        [Test]
        public void EveryRoundGivesEachUnitOneTurnAndSeedReproducesOrder()
        {
            BattleSimulation simulation = CreateSixUnitBattle();
            BattleSimulation sameSeed = CreateSixUnitBattle();
            simulation.TryStartBattle();
            sameSeed.TryStartBattle();

            for (int round = 1; round <= 5; round++)
            {
                HashSet<int> seen = new HashSet<int>();

                for (int i = 0; i < 6; i++)
                {
                    Assert.That(simulation.RoundNumber, Is.EqualTo(round));
                    Assert.That(simulation.CurrentUnitId, Is.EqualTo(sameSeed.CurrentUnitId));
                    int unitId = simulation.CurrentUnitId.Value;
                    Assert.That(seen.Add(unitId), Is.True);
                    simulation.Board.TryGetUnit(unitId, out HexCharacter unit);
                    Assert.That(unit.ActionPoints, Is.EqualTo(round == 1 ? 4 : 8));
                    Assert.That(unit.MovementRemaining, Is.EqualTo(2));
                    Assert.That(simulation.TryEndTurn(unitId), Is.True);
                    Assert.That(sameSeed.TryEndTurn(unitId), Is.True);
                }

                Assert.That(seen.Count, Is.EqualTo(6));
                Assert.That(simulation.RoundNumber, Is.EqualTo(round + 1));
            }
        }

        [Test]
        public void RemovingFutureUnitSkipsOnlyItsTurnInRoundSnapshot()
        {
            int[] order = ReadFirstRoundOrder();
            BattleSimulation simulation = CreateSixUnitBattle();
            simulation.TryStartBattle();
            Assert.That(simulation.RemoveUnit(order[2]), Is.True);

            for (int i = 0; i < order.Length; i++)
            {
                if (i == 2)
                {
                    continue;
                }

                Assert.That(simulation.RoundNumber, Is.EqualTo(1));
                Assert.That(simulation.CurrentUnitId, Is.EqualTo(order[i]));
                Assert.That(simulation.TryEndTurn(order[i]), Is.True);
            }

            Assert.That(simulation.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void RemovingCurrentUnitStartsNextSurvivorWithoutRepeatingEarlierTurns()
        {
            int[] order = ReadFirstRoundOrder();
            BattleSimulation simulation = CreateSixUnitBattle();
            simulation.TryStartBattle();
            Assert.That(simulation.RemoveUnit(order[0]), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(order[0]));
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.TrySettleAction(), Is.False);

            for (int i = 1; i < order.Length; i++)
            {
                Assert.That(simulation.RoundNumber, Is.EqualTo(1));
                Assert.That(simulation.CurrentUnitId, Is.EqualTo(order[i]));
                simulation.Board.TryGetUnit(order[i], out HexCharacter unit);
                Assert.That(unit.ActionPoints, Is.EqualTo(4));
                Assert.That(simulation.TryEndTurn(order[i]), Is.True);
            }

            Assert.That(simulation.RoundNumber, Is.EqualTo(2));
        }

        [TestCase(0, 8)]
        [TestCase(1, 6)]
        [TestCase(2, 4)]
        public void ActionPointsCarryOverAndCapOnFollowingOwnTurn(int attacks, int expectedActionPoints)
        {
            BattleSimulation simulation = CreateAdjacentBattle(out HexCharacter player, out HexCharacter enemy,
                stats: BattleTestUtility.CreateStats(maxHealth: 100));
            BattleTestUtility.StartTurn(simulation, player.Id);

            for (int i = 0; i < attacks; i++)
            {
                CompleteAttack(simulation, player.Id, enemy.Id);
            }

            Assert.That(player.ActionPoints, Is.EqualTo(4 - attacks * 2));
            Assert.That(simulation.TryEndTurn(player.Id), Is.True);
            BattleTestUtility.AdvanceToTurn(simulation, player.Id);
            Assert.That(player.ActionPoints, Is.EqualTo(expectedActionPoints));
            Assert.That(simulation.TryEndTurn(player.Id), Is.True);
            BattleTestUtility.AdvanceToTurn(simulation, player.Id);
            Assert.That(player.ActionPoints, Is.EqualTo(8));
        }

        [Test]
        public void MovementResetsEachOwnTurnWithoutCarryover()
        {
            BattleSimulation simulation = CreateAdjacentBattle(out HexCharacter player, out _);
            BattleTestUtility.StartTurn(simulation, player.Id);
            CompleteMove(simulation, player.Id, new HexCoordinate(0, 1));
            Assert.That(player.MovementRemaining, Is.EqualTo(1));
            Assert.That(simulation.TryEndTurn(player.Id), Is.True);
            BattleTestUtility.AdvanceToTurn(simulation, player.Id);
            Assert.That(player.MovementRemaining, Is.EqualTo(2));
        }

        [Test]
        public void MovementAndAttacksCanBeInterleavedUntilIndependentBudgetsAreExhausted()
        {
            BattleSimulation simulation = new BattleSimulation(3, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter player);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter enemy);
            BattleTestUtility.StartTurn(simulation, player.Id);
            CompleteMove(simulation, player.Id, new HexCoordinate(1, 0));
            Assert.That(player.ActionPoints, Is.EqualTo(4));
            CompleteAttack(simulation, player.Id, enemy.Id);
            Assert.That(player.MovementRemaining, Is.EqualTo(1));
            CompleteMove(simulation, player.Id, new HexCoordinate(1, 1));
            CompleteAttack(simulation, player.Id, enemy.Id);
            Assert.That(player.MovementRemaining, Is.Zero);
            Assert.That(player.ActionPoints, Is.Zero);
            Assert.That(enemy.Health, Is.EqualTo(4));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(player.Id));
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(0, 1)), Is.False);
            Assert.That(simulation.TryAttack(player.Id, enemy.Id), Is.False);
            Assert.That(simulation.TryEndTurn(player.Id), Is.True);
        }

        [Test]
        public void NoActionPointsStillAllowsMovementAndNoMovementStillAllowsAdjacentAttack()
        {
            BattleSimulation noAp = CreateAdjacentBattle(out HexCharacter mover, out HexCharacter target,
                new BattleRules(startingActionPoints: 0));
            BattleTestUtility.StartTurn(noAp, mover.Id);
            Assert.That(noAp.TryAttack(mover.Id, target.Id), Is.False);
            CompleteMove(noAp, mover.Id, new HexCoordinate(0, 1));
            Assert.That(mover.ActionPoints, Is.Zero);

            BattleSimulation noMovement = CreateAdjacentBattle(out HexCharacter attacker, out HexCharacter enemy,
                stats: BattleTestUtility.CreateStats(movementPerTurn: 0));
            BattleTestUtility.StartTurn(noMovement, attacker.Id);
            Assert.That(noMovement.TryMove(attacker.Id, new HexCoordinate(0, 1)), Is.False);
            CompleteAttack(noMovement, attacker.Id, enemy.Id);
            Assert.That(attacker.MovementRemaining, Is.Zero);
            Assert.That(enemy.Health, Is.EqualTo(7));
        }

        [Test]
        public void OverBudgetMovementRejectsWholePathWithoutSpendingResources()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter player);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(3, 0)), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(player.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(player.MovementRemaining, Is.EqualTo(2));
            Assert.That(player.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(2, 0)), Is.True);
            simulation.CancelAction();
            Assert.That(player.MovementRemaining, Is.EqualTo(2));
            CompleteMove(simulation, player.Id, new HexCoordinate(2, 0));
            Assert.That(player.MovementRemaining, Is.Zero);
        }

        [TestCase(4, 4)]
        [TestCase(3, 1)]
        public void ApproachRequiresEntireMovementBudgetAndAttackCostBeforeStarting(int targetQ, int actionPoints)
        {
            BattleSimulation simulation = new BattleSimulation(5, 1,
                new BattleRules(startingActionPoints: actionPoints), 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter player);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(targetQ, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryAttack(player.Id, target.Id), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(player.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(player.ActionPoints, Is.EqualTo(actionPoints));
            Assert.That(player.MovementRemaining, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(10));
        }

        [Test]
        public void ExactBudgetApproachChargesActionPointsOnlyAtAttackStartAndDamageOnlyAtCompletion()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter player);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryAttack(player.Id, target.Id), Is.True);

            for (int i = 0; i < 2; i++)
            {
                Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
                Assert.That(player.ActionPoints, Is.EqualTo(4));
                Assert.That(simulation.TryBeginAttack(out _), Is.False);
                Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(player.MovementRemaining, Is.Zero);
            Assert.That(player.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(player.ActionPoints, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(10));
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.TargetId, Is.EqualTo(target.Id));
            Assert.That(result.Damage, Is.EqualTo(3));
            Assert.That(result.RemainingHealth, Is.EqualTo(7));
            Assert.That(result.TargetDied, Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(target.Health, Is.EqualTo(7));
            Assert.That(player.ActionPoints, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancellationOnlyDiscardsAttackBeforeStepIsIssued(bool beginAttack)
        {
            BattleSimulation simulation = CreateAdjacentBattle(out HexCharacter player, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryAttack(player.Id, target.Id), Is.True);

            if (beginAttack)
            {
                Assert.That(simulation.TryBeginAttack(out _), Is.True);
            }

            simulation.CancelAction();
            Assert.That(simulation.IsBusy, Is.EqualTo(beginAttack));
            Assert.That(player.ActionPoints, Is.EqualTo(beginAttack ? 2 : 4));
            Assert.That(target.Health, Is.EqualTo(10));
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.EqualTo(beginAttack));
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(player.ActionPoints, Is.EqualTo(beginAttack ? 2 : 4));
            Assert.That(target.Health, Is.EqualTo(beginAttack ? 7 : 10));
        }

        [Test]
        public void FatalAttackCompletesNormallyThenWaitsAndRejectsDuplicateDamage()
        {
            BattleSimulation simulation = CreateAdjacentBattle(out HexCharacter player, out HexCharacter target,
                stats: BattleTestUtility.CreateStats(maxHealth: 1, attackDamage: int.MaxValue));
            BattleTestUtility.StartTurn(simulation, player.Id);
            simulation.TryAttack(player.Id, target.Id);
            simulation.TryBeginAttack(out _);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Running));
            Assert.That(simulation.Board.TryGetUnit(target.Id, out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.TargetDied, Is.True);
            Assert.That(result.RemainingHealth, Is.Zero);
            Assert.That(result.Damage, Is.EqualTo(1));
            Assert.That(target.Health, Is.Zero);
            Assert.That(simulation.Board.IsOccupied(target.Coordinate), Is.False);
            Assert.That(simulation.Board.TryGetUnit(target.Id, out _), Is.False);
            Assert.That(player.ActionPoints, Is.EqualTo(2));
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(simulation.CurrentUnitId, Is.Null);
            int round = simulation.RoundNumber;
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(simulation.TryEndTurn(player.Id), Is.False);
            Assert.That(simulation.TryMove(player.Id, target.Coordinate), Is.False);
            Assert.That(simulation.RoundNumber, Is.EqualTo(round));
            Assert.That(player.ActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void KilledPendingUnitIsSkippedThisRoundAndExcludedFromNextRound()
        {
            BattleSimulation simulation = new BattleSimulation(2, 2, randomSeed: 7);
            BattleTestUnit stats = BattleTestUtility.CreateStats(attackDamage: 10);
            simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East, out _);
            simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 0), stats, HexDirection.East, out _);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1), stats, HexDirection.East, out _);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 1), stats, HexDirection.East, out _);
            simulation.TryStartBattle();
            simulation.Board.TryGetUnit(simulation.CurrentUnitId.Value, out HexCharacter attacker);
            HexCharacter target = null;

            foreach (HexCharacter unit in simulation.Board.Units)
            {
                if (unit.Team != attacker.Team && unit.Coordinate.DistanceTo(attacker.Coordinate) == 1)
                {
                    target = unit;
                    break;
                }
            }

            Assert.That(target, Is.Not.Null);
            CompleteAttack(simulation, attacker.Id, target.Id);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Running));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(attacker.Id));

            for (int round = 1; round <= 2; round++)
            {
                HashSet<int> seen = new HashSet<int>();

                for (int i = 0; i < 3; i++)
                {
                    Assert.That(simulation.RoundNumber, Is.EqualTo(round));
                    int current = simulation.CurrentUnitId.Value;
                    Assert.That(current, Is.Not.EqualTo(target.Id));
                    Assert.That(seen.Add(current), Is.True);
                    Assert.That(simulation.TryEndTurn(current), Is.True);
                }

                Assert.That(simulation.RoundNumber, Is.EqualTo(round + 1));
            }
        }

        [Test]
        public void LargeActionPointGrantCapsWithoutIntegerOverflow()
        {
            BattleRules rules = new BattleRules(startingActionPoints: int.MaxValue - 1,
                maxActionPoints: int.MaxValue, actionPointsPerTurn: int.MaxValue);
            BattleSimulation simulation = CreateAdjacentBattle(out HexCharacter player, out _, rules);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(player.ActionPoints, Is.EqualTo(int.MaxValue - 1));
            simulation.TryEndTurn(player.Id);
            BattleTestUtility.AdvanceToTurn(simulation, player.Id);
            Assert.That(player.ActionPoints, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void InvalidRulesAreRejectedAndZeroRegenerationAndMovementAreValid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleRules(maxPlayerUnits: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleRules(maxActionPoints: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleRules(startingActionPoints: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleRules(startingActionPoints: 9));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleRules(actionPointsPerTurn: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleTestUtility.CreateStats(movementPerTurn: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleTestUtility.CreateStats(maxHealth: 0));
            Assert.Throws<ArgumentException>(() => BattleTestUtility.CreateStats(attackDamage: 0));
            Assert.DoesNotThrow(() => new BattleRules(startingActionPoints: 0, actionPointsPerTurn: 0));
            Assert.DoesNotThrow(() => BattleTestUtility.CreateStats(movementPerTurn: 0));
        }

        static BattleSimulation CreateAdjacentBattle(out HexCharacter player, out HexCharacter enemy,
            BattleRules rules = null, BattleTestUnit stats = null)
        {
            BattleSimulation simulation = new BattleSimulation(3, 2, rules, 7);
            stats = stats ?? BattleTestUtility.CreateStats();
            simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East, out player);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0), stats, HexDirection.East, out enemy);
            return simulation;
        }

        static BattleSimulation CreateSixUnitBattle()
        {
            BattleSimulation simulation = new BattleSimulation(6, 1, randomSeed: 7);

            for (int q = 0; q < 6; q++)
            {
                simulation.TrySpawnUnit(q < 3 ? BattleTeam.Player : BattleTeam.Enemy, new HexCoordinate(q, 0),
                    BattleTestUtility.CreateStats(), HexDirection.East, out _);
            }

            return simulation;
        }

        static int[] ReadFirstRoundOrder()
        {
            BattleSimulation simulation = CreateSixUnitBattle();
            simulation.TryStartBattle();
            int[] order = new int[simulation.Board.Units.Count];

            for (int i = 0; i < order.Length; i++)
            {
                order[i] = simulation.CurrentUnitId.Value;
                simulation.TryEndTurn(order[i]);
            }

            return order;
        }

        static void CompleteAttack(BattleSimulation simulation, int attackerId, int targetId)
        {
            Assert.That(simulation.TryAttack(attackerId, targetId), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
        }

        static void CompleteMove(BattleSimulation simulation, int unitId, HexCoordinate destination)
        {
            Assert.That(simulation.TryMove(unitId, destination), Is.True);
            simulation.Board.TryGetUnit(unitId, out HexCharacter unit);
            int remaining = unit.MovementRemaining;

            while (simulation.TryBeginMovementStep(out _))
            {
                Assert.That(remaining--, Is.GreaterThan(0));
                Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(unit.Coordinate, Is.EqualTo(destination));
        }
    }
}
