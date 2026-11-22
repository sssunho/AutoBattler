using System;
using RaggedBanner.HexGrid;
using NUnit.Framework;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSimulationTests
    {
        [Test]
        public void MoveAdvancesOneHexAndUpdatesOccupancyBeforeWaiting()
        {
            BattleSimulation simulation = CreateMovementBattle(4, out HexCharacter character, 3);
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(3, 0)), Is.True);
            Assert.That(character.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(character.MovementRemaining, Is.EqualTo(3));
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.False);

            for (int q = 1; q <= 3; q++)
            {
                Assert.That(simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
                Assert.That(step.UnitId, Is.EqualTo(character.Id));
                Assert.That(step.From, Is.EqualTo(new HexCoordinate(q - 1, 0)));
                Assert.That(step.To, Is.EqualTo(new HexCoordinate(q, 0)));
                Assert.That(character.Coordinate, Is.EqualTo(step.To));
                Assert.That(character.MovementRemaining, Is.EqualTo(3 - q));
                Assert.That(character.ActionPoints, Is.EqualTo(4));
                Assert.That(simulation.Board.IsOccupied(step.From), Is.False);
                Assert.That(simulation.Board.TryGetUnit(step.To, out HexCharacter occupant), Is.True);
                Assert.That(occupant, Is.SameAs(character));
                Assert.That(simulation.IsBusy, Is.True);
                Assert.That(simulation.IsWaitingForMovementCompletion, Is.True);
                Assert.That(simulation.TryMove(character.Id, default), Is.False);
                Assert.That(simulation.TryEndTurn(character.Id), Is.False);
                Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
                Assert.That(simulation.TryCompleteMovementStep(), Is.True);
                Assert.That(simulation.IsWaitingForMovementCompletion, Is.False);
                Assert.That(simulation.TryCompleteMovementStep(), Is.False);
            }

            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(character.Id));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
        }

        [Test]
        public void TurnsRejectOtherUnitsAndAdvanceOncePerParticipant()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter player);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter enemy);
            Assert.That(simulation.TryStartBattle(), Is.True);
            simulation.Board.TryGetUnit(simulation.CurrentUnitId.Value, out HexCharacter first);
            HexCharacter second = first == player ? enemy : player;
            HexCoordinate secondDestination = second == player ? new HexCoordinate(1, 0) : new HexCoordinate(2, 1);
            Assert.That(simulation.TryMove(second.Id, secondDestination), Is.False);
            Assert.That(simulation.TryEndTurn(second.Id), Is.False);
            Assert.That(simulation.TryEndTurn(first.Id), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(second.Id));
            Assert.That(simulation.TryMove(second.Id, secondDestination), Is.True);
            Assert.That(simulation.TryMove(first.Id, new HexCoordinate(1, 0)), Is.False);
            Assert.That(simulation.TryEndTurn(second.Id), Is.False);
            simulation.CancelAction();
            Assert.That(simulation.TryEndTurn(second.Id), Is.True);
            Assert.That(simulation.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void PauseBeforeFirstStepPreservesPositionAndBlocksTurnEnd()
        {
            BattleSimulation simulation = CreateMovementBattle(3, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(2, 0));
            Assert.That(simulation.PauseMovement(), Is.True);
            Assert.That(simulation.PauseMovement(), Is.False);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(character.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(character.MovementRemaining, Is.EqualTo(2));
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(1, 0)), Is.False);
            Assert.That(simulation.TryEndTurn(character.Id), Is.False);
            Assert.That(simulation.ResumeMovement(), Is.True);
            Assert.That(simulation.ResumeMovement(), Is.False);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new HexCoordinate(1, 0)));
        }

        [Test]
        public void PauseDuringStepAllowsCompletionButBlocksNextHex()
        {
            BattleSimulation simulation = CreateMovementBattle(3, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(2, 0));
            simulation.TryBeginMovementStep(out HexMovementStep first);
            simulation.PauseMovement();
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.IsMovementPaused, Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.ResumeMovement(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep second), Is.True);
            Assert.That(second.From, Is.EqualTo(first.To));
        }

        [Test]
        public void ResumeDoesNotBypassPendingStepCompletion()
        {
            BattleSimulation simulation = CreateMovementBattle(3, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(2, 0));
            simulation.TryBeginMovementStep(out _);
            simulation.PauseMovement();
            simulation.ResumeMovement();
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
        }

        [TestCase(1, false)]
        [TestCase(3, false)]
        [TestCase(3, true)]
        [TestCase(5, false)]
        public void CancellationPreservesIssuedStepAndDiscardsRemainingPath(int issuedSteps, bool pause)
        {
            BattleSimulation simulation = CreateMovementBattle(6, out HexCharacter character, 5);
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(5, 0)), Is.True);
            HexMovementStep step = default;

            for (int i = 0; i < issuedSteps; i++)
            {
                Assert.That(simulation.TryBeginMovementStep(out step), Is.True);

                if (i < issuedSteps - 1)
                {
                    Assert.That(simulation.TryCompleteMovementStep(), Is.True);
                }
            }

            if (pause)
            {
                Assert.That(simulation.PauseMovement(), Is.True);
            }

            simulation.CancelAction();
            simulation.CancelAction();
            Assert.That(character.Coordinate, Is.EqualTo(step.To));
            Assert.That(character.MovementRemaining, Is.EqualTo(5 - issuedSteps));
            Assert.That(character.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.Board.IsOccupied(step.From), Is.False);
            Assert.That(simulation.Board.TryGetUnit(step.To, out HexCharacter occupant), Is.True);
            Assert.That(occupant, Is.SameAs(character));
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Moving));
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(simulation.IsMovementPaused, Is.False);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryMove(character.Id, step.From), Is.False);
            Assert.That(simulation.TryEndTurn(character.Id), Is.False);

            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.False);
            Assert.That(character.Coordinate, Is.EqualTo(step.To));
            Assert.That(character.MovementRemaining, Is.EqualTo(5 - issuedSteps));
            Assert.That(simulation.TryEndTurn(character.Id), Is.True);
        }

        [TestCase(0)]
        [TestCase(2)]
        public void CancellationWithoutIssuedStepImmediatelyClearsPlannedPath(int completedSteps)
        {
            BattleSimulation simulation = CreateMovementBattle(6, out HexCharacter character, 5);
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(5, 0)), Is.True);

            for (int i = 0; i < completedSteps; i++)
            {
                Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
                Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(simulation.PauseMovement(), Is.True);
            simulation.CancelAction();
            simulation.CancelAction();
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(simulation.IsMovementPaused, Is.False);
            Assert.That(character.Coordinate, Is.EqualTo(new HexCoordinate(completedSteps, 0)));
            Assert.That(character.MovementRemaining, Is.EqualTo(5 - completedSteps));
            Assert.That(simulation.TryCompleteMovementStep(), Is.False);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(5, 0)), Is.True);
        }

        [Test]
        public void FinalStepCompletionClearsPauseAndAllowsAnotherCommand()
        {
            BattleSimulation simulation = CreateMovementBattle(2, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(1, 0));
            simulation.TryBeginMovementStep(out _);
            simulation.PauseMovement();
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsMovementPaused, Is.False);
            Assert.That(simulation.TryMove(character.Id, default), Is.True);
        }

        [Test]
        public void OccupiedDestinationAndBlockedCorridorRejectMovement()
        {
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter mover);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out _);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(1, 0)), Is.False);
            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(2, 0)), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(mover.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(mover.MovementRemaining, Is.EqualTo(2));
        }

        [Test]
        public void PathDetoursAroundOccupiedHex()
        {
            BattleSimulation simulation = new BattleSimulation(3, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter mover);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter blocker);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(2, 0)), Is.True);
            int steps = 0;

            while (simulation.TryBeginMovementStep(out HexMovementStep step))
            {
                Assert.That(step.To, Is.Not.EqualTo(blocker.Coordinate));
                simulation.TryCompleteMovementStep();
                steps++;
                Assert.That(steps, Is.LessThanOrEqualTo(3));
            }

            Assert.That(steps, Is.EqualTo(3));
            Assert.That(mover.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
        }

        [Test]
        public void RemovingMovingUnitReleasesOccupancyAndAdvancesTurn()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter first);
            simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(3, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter second);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _);
            BattleTestUtility.StartTurn(simulation, first.Id);
            simulation.TryMove(first.Id, new HexCoordinate(2, 0));
            simulation.TryBeginMovementStep(out _);
            Assert.That(simulation.RemoveUnit(first.Id), Is.True);
            Assert.That(simulation.Board.IsOccupied(first.Coordinate), Is.False);
            Assert.That(simulation.Board.TryGetUnit(first.Id, out _), Is.False);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(first.Id));
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(simulation.TryEndTurn(first.Id), Is.False);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.TrySettleAction(), Is.False);
            Assert.That(simulation.CurrentUnitId, Is.Not.Null.And.Not.EqualTo(first.Id));
            Assert.That(simulation.State, Is.EqualTo(BattleState.Running));
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.False);
            Assert.That(simulation.RemoveUnit(second.Id), Is.True);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(simulation.CurrentUnitId, Is.Null);
            Assert.That(simulation.TryEndTurn(second.Id), Is.False);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);
        }

        [Test]
        public void RemovingCompletedAndFutureUnitsPreservesCurrentTurnAndResources()
        {
            BattleSimulation simulation = new BattleSimulation(6, 1, randomSeed: 7);

            for (int q = 0; q < 6; q++)
            {
                simulation.TrySpawnUnit(q < 3 ? BattleTeam.Player : BattleTeam.Enemy, new HexCoordinate(q, 0),
                    BattleTestUtility.CreateStats(), HexDirection.East, out _);
            }

            simulation.TryStartBattle();
            int completedId = simulation.CurrentUnitId.Value;
            simulation.TryEndTurn(completedId);
            int currentId = simulation.CurrentUnitId.Value;
            simulation.Board.TryGetUnit(currentId, out HexCharacter current);
            int futureId = 0;

            foreach (HexCharacter unit in simulation.Board.Units)
            {
                if (unit.Id != completedId && unit.Id != currentId)
                {
                    futureId = unit.Id;
                    break;
                }
            }

            Assert.That(simulation.RemoveUnit(completedId), Is.True);
            Assert.That(simulation.RemoveUnit(futureId), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(currentId));
            Assert.That(simulation.RoundNumber, Is.EqualTo(1));
            Assert.That(current.ActionPoints, Is.EqualTo(4));
            Assert.That(current.MovementRemaining, Is.EqualTo(2));
            Assert.That(simulation.TryEndTurn(currentId), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.Not.EqualTo(completedId).And.Not.EqualTo(futureId));
        }

        [Test]
        public void SpawnRejectsInvalidPlacementsAndAllRegistrationAfterStart()
        {
            BattleSimulation simulation = new BattleSimulation(3, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter character);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(-1, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);
            Assert.That(simulation.TrySpawnUnit((BattleTeam)999, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.True);
            BattleTestUtility.StartTurn(simulation, character.Id);
            simulation.TryMove(character.Id, new HexCoordinate(2, 0));
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);
            simulation.CancelAction();
            Assert.That(simulation.Board.Units.Count, Is.EqualTo(2));
            Assert.That(character.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.False);
        }

        [Test]
        public void InvalidCommandsDoNotCreateAnAction()
        {
            BattleSimulation simulation = new BattleSimulation(2, 2, randomSeed: 7);
            Assert.That(simulation.TryEndTurn(0), Is.False);
            Assert.That(simulation.TryMove(0, default), Is.False);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter character);
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(1, 0)), Is.False);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _);
            BattleTestUtility.StartTurn(simulation, character.Id);
            Assert.That(simulation.TryMove(character.Id, default), Is.False);
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(2, 0)), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.False);
            Assert.That(simulation.PauseMovement(), Is.False);
            Assert.That(simulation.ResumeMovement(), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.RemoveUnit(-1), Is.False);
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        public void InvalidMapDimensionsAreRejected(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleSimulation(width, height));
        }

        static BattleSimulation CreateMovementBattle(int width, out HexCharacter character, int movement = 2)
        {
            BattleSimulation simulation = new BattleSimulation(width, 2, randomSeed: 7);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: movement), HexDirection.East, out character), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(width - 1, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.True);
            BattleTestUtility.StartTurn(simulation, character.Id);
            return simulation;
        }
    }
}
