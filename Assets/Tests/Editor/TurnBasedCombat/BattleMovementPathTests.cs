using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleMovementPathTests
    {
        [Test]
        public void CancelledIssuedMovementCanCompleteOnceAndTheNextMovementStartsCleanly()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 2));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(8, 8));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryMove(actor.Id, new HexCoordinate(4, 2)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.PauseMovement(), Is.True);
            battle.CancelAction();
            Assert.That(battle.IsWaitingForMovementCompletion, Is.True);
            Assert.That(battle.IsMovementPaused, Is.False);
            Assert.That(battle.TryCopyRemainingMovementPath(new List<HexCoordinate>()), Is.False);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.False);
            Assert.That(battle.TryMove(actor.Id, new HexCoordinate(4, 2)), Is.True);
            Assert.That(battle.IsMovementPaused, Is.False);
            Assert.That(battle.IsWaitingForMovementCompletion, Is.False);
            Assert.That(battle.TryBeginMovementStep(out HexMovementStep next), Is.True);
            Assert.That(next.From, Is.EqualTo(new HexCoordinate(3, 2)));
            Assert.That(next.To, Is.EqualTo(new HexCoordinate(4, 2)));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(actor.MovementRemaining, Is.EqualTo(4));
        }

        [Test]
        public void EqualLengthPathsFollowTheLineWithoutChoosingLongerRoutes()
        {
            HexPathfinder finder = new HexPathfinder();
            BattleBoard board = new BattleBoard(8, 8);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(finder.TryFindPath(board, default, new HexCoordinate(2, 2), 4,
                new BattlePathLine(0d, 1d), path), Is.True);
            Assert.That(path, Is.EqualTo(new[]
            {
                new HexCoordinate(1, 0), new HexCoordinate(2, 0),
                new HexCoordinate(2, 1), new HexCoordinate(2, 2)
            }));
            Assert.That(finder.TryFindPath(board, default, new HexCoordinate(2, 2), 4,
                new BattlePathLine(1d, 0d), path), Is.True);
            Assert.That(path, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 1), new HexCoordinate(0, 2),
                new HexCoordinate(1, 2), new HexCoordinate(2, 2)
            }));
            Assert.That(finder.TryFindPath(board, new HexCoordinate(2, 2), new HexCoordinate(4, 2), 6,
                new BattlePathLine(1d, 0d), path), Is.True);
            Assert.That(path.Count, Is.EqualTo(2));
        }

        [Test]
        public void OrdinaryMovementAllowsDetoursButRespectsTheMovementBudget()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 2));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 2));
            BattleTestUtility.StartTurn(battle, actor.Id);
            HexPathfinder finder = new HexPathfinder();
            List<HexCoordinate> path = new List<HexCoordinate>();
            HexCoordinate destination = new HexCoordinate(4, 2);
            Assert.That(finder.TryFindPath(battle.Board, actor.Coordinate, destination, 2,
                new BattlePathLine(0d, 1d), path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(finder.TryFindPath(battle.Board, actor.Coordinate, destination, 3,
                new BattlePathLine(0d, 1d), path), Is.True);
            Assert.That(path.Count, Is.EqualTo(3));
            Assert.That(path[0].DistanceTo(destination), Is.EqualTo(actor.Coordinate.DistanceTo(destination)));
            Assert.That(path.Contains(new HexCoordinate(3, 2)), Is.False);
        }

        [Test]
        public void AttackSearchComparesAllGoalsAtTheShortestDepth()
        {
            HexPathfinder finder = new HexPathfinder();
            BattleBoard board = new BattleBoard(8, 8);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(finder.TryFindPathToAttackPosition(board, new HexCoordinate(2, 2),
                new HexCoordinate(4, 1), 1, 1, new BattlePathLine(1d, 1d), path), Is.True);
            Assert.That(path, Is.EqualTo(new[] { new HexCoordinate(3, 1) }));
        }

        [Test]
        public void FrozenMovementOwnsItsCopyAndRemainingPathTracksCompletionPauseAndCancel()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 2));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(8, 8));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            HexCoordinate destination = new HexCoordinate(4, 4);
            Assert.That(battle.TryCopyMovementPath(actor.Id, destination, new BattlePathLine(0d, 1d), path), Is.True);
            List<HexCoordinate> expected = new List<HexCoordinate>(path);
            Assert.That(battle.TryMove(actor.Id, destination, path), Is.True);
            path.Clear();
            Assert.That(battle.TryMove(actor.Id, new HexCoordinate(3, 2)), Is.False);
            Assert.That(battle.TryBeginMovementStep(out HexMovementStep first), Is.True);
            Assert.That(first.To, Is.EqualTo(expected[0]));
            Assert.That(battle.TryCopyRemainingMovementPath(path), Is.True);
            Assert.That(path, Is.EqualTo(expected));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            expected.RemoveAt(0);
            Assert.That(battle.PauseMovement(), Is.True);
            Assert.That(battle.TryCopyRemainingMovementPath(path), Is.True);
            Assert.That(path, Is.EqualTo(expected));
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.ResumeMovement(), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            battle.CancelAction();
            Assert.That(battle.TryCopyRemainingMovementPath(path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(actor.MovementRemaining, Is.EqualTo(4));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ABlockedLaterCellCancelsBeforeTheFirstMovementOrAttackCost(bool attack)
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 2));
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(6, 2));
            HexCharacter blocker = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 3));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            BattlePathLine line = new BattlePathLine(0d, 1d);

            if (attack)
            {
                Assert.That(battle.TryCopyAttackPath(actor.Id, target.Id, line, path), Is.True);
                Assert.That(battle.TryAttack(actor.Id, target.Id, path), Is.True);
            }
            else
            {
                Assert.That(battle.TryCopyMovementPath(actor.Id, new HexCoordinate(5, 2), line, path), Is.True);
                Assert.That(battle.TryMove(actor.Id, new HexCoordinate(5, 2), path), Is.True);
            }

            MoveExternally(battle, blocker.Id, new HexCoordinate(4, 2));
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(2, 2)));
            Assert.That(actor.MovementRemaining, Is.EqualTo(6));
            Assert.That(actor.ActionPoints, Is.EqualTo(4));
            Assert.That(battle.TryCopyRemainingMovementPath(path), Is.False);
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void FrozenApproachUsesThePreviewThenIssuesTheAttack()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 2));
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 1));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.TryCopyAttackPath(actor.Id, target.Id, new BattlePathLine(1d, 1d), path), Is.True);
            Assert.That(path, Is.EqualTo(new[] { new HexCoordinate(3, 1) }));
            Assert.That(battle.TryAttack(actor.Id, target.Id, path), Is.True);
            path.Clear();
            Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new HexCoordinate(3, 1)));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryCopyRemainingMovementPath(path), Is.False);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void ExistingInRangeAttackNeedsNoMovementEvenAtZeroMovement()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 2), movement: 0);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 2));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.TryCopyAttackPath(actor.Id, target.Id, new BattlePathLine(0d, 1d), path), Is.True);
            Assert.That(path, Is.Empty);
            Assert.That(battle.TryAttack(actor.Id, target.Id, path), Is.True);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
        }

        [Test]
        public void OpportunityAttackStillRejectsMovementAndClearsItsDisplayedPath()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 2));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(1, 2), opportunity: true);
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.TryCopyMovementPath(actor.Id, new HexCoordinate(4, 2),
                new BattlePathLine(0d, 1d), path), Is.True);
            Assert.That(battle.TryMove(actor.Id, new HexCoordinate(4, 2), path), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
            Assert.That(battle.TryCopyRemainingMovementPath(path), Is.False);
            Assert.That(actor.MovementRemaining, Is.EqualTo(6));
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(2, 2)));
        }

        static BattleSimulation NewBattle()
        {
            return new BattleSimulation(12, 12, randomSeed: 7);
        }

        static HexCharacter Spawn(BattleSimulation battle, BattleTeam team, HexCoordinate coordinate,
            int movement = 6, bool opportunity = false)
        {
            TraitCollection traits = opportunity
                ? new TraitCollection(new[] { new Trait(TraitType.OpportunityAttack,
                    new TraitSource(TraitSourceKind.Class, 0)) }) : null;
            Assert.That(battle.TrySpawnUnit(team, coordinate,
                new BattleTestUnit(MercenaryClass.Warrior, 20, 3, movement, 1, 0,
                    initiative: team == BattleTeam.Player ? 20 : 10),
                HexDirection.East, out HexCharacter unit, traits), Is.True);
            return unit;
        }

        static void MoveExternally(BattleSimulation battle, int unitId, HexCoordinate destination)
        {
            Assert.That((bool)battle.Board.GetType().GetMethod("TryMove", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(battle.Board, new object[] { unitId, destination }), Is.True);
        }
    }
}
