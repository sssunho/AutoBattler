using System.Collections.Generic;
using RaggedBanner.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattlePathfinderTests
    {
        readonly AutoBattlerBattlePathfinder _pathfinder = new AutoBattlerBattlePathfinder();
        readonly List<HexCoordinate> _path = new List<HexCoordinate>();

        [Test]
        public void TryFindPathToAttackPosition_ReturnsShortestPath()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.True);
            Assert.That(_path, Is.EqualTo(new[]
            {
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)
            }));
        }

        [Test]
        public void TryFindPathToAttackPosition_ReturnsEmptyPathWhenAlreadyInRange()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);
            _path.Add(new HexCoordinate(1, 1));

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.True);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_AvoidsOccupiedCoordinates()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 2));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0));
            AutoBattlerBattleUnit blocker = CreateUnit(3, AutoBattlerTeam.Player, new HexCoordinate(1, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);
            board.TryPlace(blocker, blocker.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.True);
            Assert.That(_path, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 1),
                new HexCoordinate(1, 1)
            }));
        }

        [Test]
        public void TryFindPathToAttackPosition_ReturnsFalseWhenNoRouteExists()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0));
            AutoBattlerBattleUnit blocker = CreateUnit(3, AutoBattlerTeam.Player, new HexCoordinate(1, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);
            board.TryPlace(blocker, blocker.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.False);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_RejectsUnregisteredUnit()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0));
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.False);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_RejectsFriendlyTarget()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Player, new HexCoordinate(2, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.False);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_DoesNotUseOccupiedTargetCoordinate()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit mover = new AutoBattlerBattleUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 0, 1f);
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.False);
            Assert.That(_path, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TryFindPathToAttackPosition_PrefersHigherIdAcrossTheWholeNearestDepth(bool reverseTargets)
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(7, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(3, 0));
            AutoBattlerBattleUnit highId = CreateUnit(9, AutoBattlerTeam.Enemy, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit lowId = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(6, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(lowId, lowId.Coordinate);
            board.TryPlace(highId, highId.Coordinate);
            AutoBattlerBattleUnit[] targets = reverseTargets ? new[] { highId, lowId } : new[] { lowId, highId };

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, targets, _path, out AutoBattlerBattleUnit selected);

            Assert.That(found, Is.True);
            Assert.That(selected, Is.SameAs(highId));
            Assert.That(_path, Is.EqualTo(new[]
            {
                new HexCoordinate(2, 0),
                new HexCoordinate(1, 0)
            }));
        }

        [Test]
        public void TryFindPathToAttackPosition_PrefersShorterPathOverHigherId()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(8, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(3, 0));
            AutoBattlerBattleUnit near = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit far = CreateUnit(9, AutoBattlerTeam.Enemy, new HexCoordinate(7, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(near, near.Coordinate);
            board.TryPlace(far, far.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new[] { far, near }, _path, out AutoBattlerBattleUnit selected);

            Assert.That(found, Is.True);
            Assert.That(selected, Is.SameAs(near));
            Assert.That(_path.Count, Is.EqualTo(2));
            Assert.That(_path[_path.Count - 1].DistanceTo(near.Coordinate), Is.EqualTo(1));
        }

        [Test]
        public void TryFindPathToAttackPosition_SelectsReachableEnemyOverCloserBlockedEnemy()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(5, 3));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 1));
            AutoBattlerBattleUnit blocked = CreateUnit(9, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0));
            AutoBattlerBattleUnit reachable = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(4, 2));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(blocked, blocked.Coordinate);
            board.TryPlace(reachable, reachable.Coordinate);
            HexCoordinate[] blockers =
            {
                new HexCoordinate(1, 0),
                new HexCoordinate(1, 1),
                new HexCoordinate(2, 1),
                new HexCoordinate(3, 0)
            };

            for (int i = 0; i < blockers.Length; i++)
            {
                AutoBattlerBattleUnit blocker = CreateUnit(10 + i, AutoBattlerTeam.Player, blockers[i]);
                board.TryPlace(blocker, blocker.Coordinate);
            }

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new[] { blocked, reachable }, _path, out AutoBattlerBattleUnit selected);

            Assert.That(found, Is.True);
            Assert.That(selected, Is.SameAs(reachable));
            Assert.That(_path, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 2),
                new HexCoordinate(1, 2),
                new HexCoordinate(2, 2),
                new HexCoordinate(3, 2)
            }));
        }

        [Test]
        public void TryFindPathToAttackPosition_PreservesDirectionSimilarityDuringBacktracking()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(5, 3));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(4, 2));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new[] { target }, _path, out AutoBattlerBattleUnit selected);

            Assert.That(found, Is.True);
            Assert.That(selected, Is.SameAs(target));
            Assert.That(_path, Is.EqualTo(new[]
            {
                new HexCoordinate(1, 0),
                new HexCoordinate(1, 1),
                new HexCoordinate(2, 1),
                new HexCoordinate(3, 1),
                new HexCoordinate(3, 2)
            }));
        }

        [Test]
        public void TryFindPathToAttackPosition_ClearsPreviousResultWhenAllTargetsBecomeInvalid()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);
            AutoBattlerBattleUnit[] targets = { target };
            Assert.That(_pathfinder.TryFindPathToAttackPosition(board, mover, targets, _path, out _), Is.True);
            target.IsAlive = false;

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, targets, _path, out AutoBattlerBattleUnit selected);

            Assert.That(found, Is.False);
            Assert.That(selected, Is.Null);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_ReturnsFalseForEmptyCandidates()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            board.TryPlace(mover, mover.Coordinate);
            _path.Add(new HexCoordinate(1, 0));

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new AutoBattlerBattleUnit[0], _path, out AutoBattlerBattleUnit selected);

            Assert.That(found, Is.False);
            Assert.That(selected, Is.Null);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_SkipsInvalidCandidatesWhenValidEnemyExists()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit mover = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0));
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0));
            AutoBattlerBattleUnit unregistered = CreateUnit(9, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new[] { null, mover, unregistered, target }, _path, out AutoBattlerBattleUnit selected);

            Assert.That(found, Is.True);
            Assert.That(selected, Is.SameAs(target));
            Assert.That(_path.Count, Is.EqualTo(2));
        }

        static AutoBattlerBattleUnit CreateUnit(int id, AutoBattlerTeam team, HexCoordinate coordinate)
        {
            return new AutoBattlerBattleUnit(id, team, coordinate, 1, 1f);
        }
    }
}
