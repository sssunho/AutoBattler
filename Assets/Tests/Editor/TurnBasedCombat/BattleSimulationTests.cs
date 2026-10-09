using System;
using AutoBattler.HexGrid;
using NUnit.Framework;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSimulationTests
    {
        [Test]
        public void MoveAdvancesOneHexAndUpdatesOccupancyBeforeWaiting()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1);
            Assert.That(simulation.TrySpawnUnit(default, out HexCharacter character), Is.True);
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(3, 0)), Is.True);
            Assert.That(character.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));

            for (int q = 1; q <= 3; q++)
            {
                Assert.That(simulation.TryAdvanceMovement(out HexMovementStep step), Is.True);
                Assert.That(step.UnitId, Is.EqualTo(character.Id));
                Assert.That(step.From, Is.EqualTo(new HexCoordinate(q - 1, 0)));
                Assert.That(step.To, Is.EqualTo(new HexCoordinate(q, 0)));
                Assert.That(character.Coordinate, Is.EqualTo(step.To));
                Assert.That(simulation.Board.IsOccupied(step.From), Is.False);
                Assert.That(simulation.Board.TryGetUnit(step.To, out HexCharacter occupant), Is.True);
                Assert.That(occupant, Is.SameAs(character));
                Assert.That(simulation.IsBusy, Is.True);
                Assert.That(simulation.TryMove(character.Id, default), Is.False);
                Assert.That(simulation.TryEndTurn(character.Id), Is.False);
                Assert.That(simulation.TryAdvanceMovement(out _), Is.False);
                Assert.That(simulation.TryCompleteMovementStep(step.Id + 1), Is.False);
                Assert.That(simulation.TryCompleteMovementStep(step.Id), Is.True);
                Assert.That(simulation.TryCompleteMovementStep(step.Id), Is.False);
            }

            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.PendingStepId, Is.Zero);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(character.Id));
            Assert.That(simulation.TryAdvanceMovement(out _), Is.False);
        }

        [Test]
        public void TurnsFollowRegistrationOrderAndRejectOtherUnits()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2);
            simulation.TrySpawnUnit(default, out HexCharacter first);
            simulation.TrySpawnUnit(new HexCoordinate(3, 1), out HexCharacter second);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(first.Id));
            Assert.That(simulation.TryMove(second.Id, new HexCoordinate(2, 1)), Is.False);
            Assert.That(simulation.TryEndTurn(second.Id), Is.False);
            Assert.That(simulation.TryEndTurn(first.Id), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(second.Id));
            Assert.That(simulation.TryMove(second.Id, new HexCoordinate(2, 1)), Is.True);
            Assert.That(simulation.TryMove(first.Id, new HexCoordinate(1, 0)), Is.False);
            Assert.That(simulation.TryEndTurn(second.Id), Is.False);
            simulation.CancelMovement();
            Assert.That(simulation.TryEndTurn(second.Id), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(first.Id));
        }

        [Test]
        public void PauseBeforeFirstStepPreservesPositionAndBlocksTurnEnd()
        {
            BattleSimulation simulation = new BattleSimulation(3, 1);
            simulation.TrySpawnUnit(default, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(2, 0));
            Assert.That(simulation.PauseMovement(), Is.True);
            Assert.That(simulation.PauseMovement(), Is.False);
            Assert.That(simulation.TryAdvanceMovement(out _), Is.False);
            Assert.That(character.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(1, 0)), Is.False);
            Assert.That(simulation.TryEndTurn(character.Id), Is.False);
            Assert.That(simulation.ResumeMovement(), Is.True);
            Assert.That(simulation.ResumeMovement(), Is.False);
            Assert.That(simulation.TryAdvanceMovement(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new HexCoordinate(1, 0)));
        }

        [Test]
        public void PauseDuringStepAllowsCompletionButBlocksNextHex()
        {
            BattleSimulation simulation = new BattleSimulation(3, 1);
            simulation.TrySpawnUnit(default, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(2, 0));
            simulation.TryAdvanceMovement(out HexMovementStep first);
            simulation.PauseMovement();
            Assert.That(simulation.TryCompleteMovementStep(first.Id), Is.True);
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.IsMovementPaused, Is.True);
            Assert.That(simulation.TryAdvanceMovement(out _), Is.False);
            Assert.That(simulation.ResumeMovement(), Is.True);
            Assert.That(simulation.TryAdvanceMovement(out HexMovementStep second), Is.True);
            Assert.That(second.From, Is.EqualTo(first.To));
        }

        [Test]
        public void ResumeDoesNotBypassPendingStepCompletion()
        {
            BattleSimulation simulation = new BattleSimulation(3, 1);
            simulation.TrySpawnUnit(default, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(2, 0));
            simulation.TryAdvanceMovement(out HexMovementStep step);
            simulation.PauseMovement();
            simulation.ResumeMovement();
            Assert.That(simulation.TryAdvanceMovement(out _), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(step.Id), Is.True);
            Assert.That(simulation.TryAdvanceMovement(out _), Is.True);
        }

        [Test]
        public void CancellationKeepsCommittedHexAndRejectsOldCompletion()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1);
            simulation.TrySpawnUnit(default, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(3, 0));
            simulation.TryAdvanceMovement(out HexMovementStep oldStep);
            simulation.PauseMovement();
            simulation.CancelMovement();
            Assert.That(character.Coordinate, Is.EqualTo(oldStep.To));
            Assert.That(simulation.Board.IsOccupied(oldStep.To), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsMovementPaused, Is.False);
            Assert.That(simulation.TryCompleteMovementStep(oldStep.Id), Is.False);
            simulation.TryMove(character.Id, new HexCoordinate(3, 0));
            simulation.TryAdvanceMovement(out HexMovementStep nextStep);
            Assert.That(nextStep.Id, Is.Not.EqualTo(oldStep.Id));
            Assert.That(simulation.TryCompleteMovementStep(oldStep.Id), Is.False);
            Assert.That(simulation.PendingStepId, Is.EqualTo(nextStep.Id));
        }

        [Test]
        public void FinalStepCompletionClearsPauseAndAllowsAnotherCommand()
        {
            BattleSimulation simulation = new BattleSimulation(2, 1);
            simulation.TrySpawnUnit(default, out HexCharacter character);
            simulation.TryMove(character.Id, new HexCoordinate(1, 0));
            simulation.TryAdvanceMovement(out HexMovementStep step);
            simulation.PauseMovement();
            Assert.That(simulation.TryCompleteMovementStep(step.Id), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsMovementPaused, Is.False);
            Assert.That(simulation.TryMove(character.Id, default), Is.True);
        }

        [Test]
        public void OccupiedDestinationAndBlockedCorridorRejectMovement()
        {
            BattleSimulation simulation = new BattleSimulation(3, 1);
            simulation.TrySpawnUnit(default, out HexCharacter mover);
            simulation.TrySpawnUnit(new HexCoordinate(1, 0), out _);
            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(1, 0)), Is.False);
            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(2, 0)), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(mover.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
        }

        [Test]
        public void PathDetoursAroundOccupiedHex()
        {
            BattleSimulation simulation = new BattleSimulation(3, 2);
            simulation.TrySpawnUnit(default, out HexCharacter mover);
            simulation.TrySpawnUnit(new HexCoordinate(1, 0), out HexCharacter blocker);
            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(2, 0)), Is.True);
            int steps = 0;

            while (simulation.TryAdvanceMovement(out HexMovementStep step))
            {
                Assert.That(step.To, Is.Not.EqualTo(blocker.Coordinate));
                simulation.TryCompleteMovementStep(step.Id);
                steps++;
            }

            Assert.That(steps, Is.EqualTo(3));
            Assert.That(mover.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
        }

        [Test]
        public void RemovingMovingUnitReleasesOccupancyAndAdvancesTurn()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2);
            simulation.TrySpawnUnit(default, out HexCharacter first);
            simulation.TrySpawnUnit(new HexCoordinate(3, 1), out HexCharacter second);
            simulation.TryMove(first.Id, new HexCoordinate(2, 0));
            simulation.TryAdvanceMovement(out HexMovementStep oldStep);
            Assert.That(simulation.RemoveUnit(first.Id), Is.True);
            Assert.That(simulation.Board.IsOccupied(first.Coordinate), Is.False);
            Assert.That(simulation.Board.TryGetUnit(first.Id, out _), Is.False);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(second.Id));
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.TryCompleteMovementStep(oldStep.Id), Is.False);
            Assert.That(simulation.RemoveUnit(second.Id), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.Null);
            Assert.That(simulation.TryEndTurn(second.Id), Is.False);
            Assert.That(simulation.TrySpawnUnit(default, out HexCharacter replacement), Is.True);
            Assert.That(replacement.Id, Is.GreaterThan(second.Id));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(replacement.Id));
        }

        [Test]
        public void RemovingUnitsBeforeAndAfterCurrentPreservesCurrentTurn()
        {
            BattleSimulation simulation = new BattleSimulation(4, 1);
            simulation.TrySpawnUnit(default, out HexCharacter first);
            simulation.TrySpawnUnit(new HexCoordinate(1, 0), out HexCharacter second);
            simulation.TrySpawnUnit(new HexCoordinate(2, 0), out HexCharacter third);
            simulation.TryEndTurn(first.Id);
            simulation.RemoveUnit(first.Id);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(second.Id));
            simulation.RemoveUnit(third.Id);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(second.Id));
            Assert.That(simulation.TryEndTurn(second.Id), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(second.Id));
        }

        [Test]
        public void SpawnRejectsOccupiedInvalidAndBusyPlacements()
        {
            BattleSimulation simulation = new BattleSimulation(3, 2);
            simulation.TrySpawnUnit(default, out HexCharacter character);
            Assert.That(simulation.TrySpawnUnit(default, out _), Is.False);
            Assert.That(simulation.TrySpawnUnit(new HexCoordinate(-1, 0), out _), Is.False);
            simulation.TryMove(character.Id, new HexCoordinate(2, 0));
            Assert.That(simulation.TrySpawnUnit(new HexCoordinate(0, 1), out _), Is.False);
            simulation.CancelMovement();
            Assert.That(simulation.Board.Units.Count, Is.EqualTo(1));
            Assert.That(character.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(simulation.TrySpawnUnit(new HexCoordinate(0, 1), out _), Is.True);
        }

        [Test]
        public void InvalidCommandsDoNotCreateAnAction()
        {
            BattleSimulation simulation = new BattleSimulation(2, 1);
            Assert.That(simulation.TryEndTurn(0), Is.False);
            Assert.That(simulation.TryMove(0, default), Is.False);
            simulation.TrySpawnUnit(default, out HexCharacter character);
            Assert.That(simulation.TryMove(character.Id, default), Is.False);
            Assert.That(simulation.TryMove(character.Id, new HexCoordinate(2, 0)), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(0), Is.False);
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
    }
}
