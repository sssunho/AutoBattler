using System.Collections.Generic;
using AutoBattler.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattlePathfinderTests
    {
        readonly BattlePathfinder _pathfinder = new BattlePathfinder();
        readonly List<HexCoordinate> _path = new List<HexCoordinate>();

        [Test]
        public void TryFindPathToAttackPosition_ReturnsShortestPath()
        {
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0));
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
            BattleBoard board = new BattleBoard(new HexMap(2, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(1, 0));
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
            BattleBoard board = new BattleBoard(new HexMap(3, 2));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(2, 0));
            BattleUnit blocker = CreateUnit(3, Team.Player, new HexCoordinate(1, 0));
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
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0));
            BattleUnit blocker = CreateUnit(3, Team.Player, new HexCoordinate(1, 0));
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
            BattleBoard board = new BattleBoard(new HexMap(3, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(2, 0));
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.False);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_RejectsFriendlyTarget()
        {
            BattleBoard board = new BattleBoard(new HexMap(3, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Player, new HexCoordinate(2, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, new[] { target }, _path, out _);

            Assert.That(found, Is.False);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_DoesNotUseOccupiedTargetCoordinate()
        {
            BattleBoard board = new BattleBoard(new HexMap(2, 1));
            BattleUnit mover = new BattleUnit(1, Team.Player, new HexCoordinate(0, 0), 0, 1f);
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(1, 0));
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
            BattleBoard board = new BattleBoard(new HexMap(7, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(3, 0));
            BattleUnit highId = CreateUnit(9, Team.Enemy, new HexCoordinate(0, 0));
            BattleUnit lowId = CreateUnit(2, Team.Enemy, new HexCoordinate(6, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(lowId, lowId.Coordinate);
            board.TryPlace(highId, highId.Coordinate);
            BattleUnit[] targets = reverseTargets ? new[] { highId, lowId } : new[] { lowId, highId };

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, targets, _path, out BattleUnit selected);

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
            BattleBoard board = new BattleBoard(new HexMap(8, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(3, 0));
            BattleUnit near = CreateUnit(2, Team.Enemy, new HexCoordinate(0, 0));
            BattleUnit far = CreateUnit(9, Team.Enemy, new HexCoordinate(7, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(near, near.Coordinate);
            board.TryPlace(far, far.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new[] { far, near }, _path, out BattleUnit selected);

            Assert.That(found, Is.True);
            Assert.That(selected, Is.SameAs(near));
            Assert.That(_path.Count, Is.EqualTo(2));
            Assert.That(_path[_path.Count - 1].DistanceTo(near.Coordinate), Is.EqualTo(1));
        }

        [Test]
        public void TryFindPathToAttackPosition_SelectsReachableEnemyOverCloserBlockedEnemy()
        {
            BattleBoard board = new BattleBoard(new HexMap(5, 3));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 1));
            BattleUnit blocked = CreateUnit(9, Team.Enemy, new HexCoordinate(2, 0));
            BattleUnit reachable = CreateUnit(2, Team.Enemy, new HexCoordinate(4, 2));
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
                BattleUnit blocker = CreateUnit(10 + i, Team.Player, blockers[i]);
                board.TryPlace(blocker, blocker.Coordinate);
            }

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new[] { blocked, reachable }, _path, out BattleUnit selected);

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
            BattleBoard board = new BattleBoard(new HexMap(5, 3));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(4, 2));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new[] { target }, _path, out BattleUnit selected);

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
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);
            BattleUnit[] targets = { target };
            Assert.That(_pathfinder.TryFindPathToAttackPosition(board, mover, targets, _path, out _), Is.True);
            target.IsAlive = false;

            bool found = _pathfinder.TryFindPathToAttackPosition(board, mover, targets, _path, out BattleUnit selected);

            Assert.That(found, Is.False);
            Assert.That(selected, Is.Null);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_ReturnsFalseForEmptyCandidates()
        {
            BattleBoard board = new BattleBoard(new HexMap(2, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            board.TryPlace(mover, mover.Coordinate);
            _path.Add(new HexCoordinate(1, 0));

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new BattleUnit[0], _path, out BattleUnit selected);

            Assert.That(found, Is.False);
            Assert.That(selected, Is.Null);
            Assert.That(_path, Is.Empty);
        }

        [Test]
        public void TryFindPathToAttackPosition_SkipsInvalidCandidatesWhenValidEnemyExists()
        {
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit mover = CreateUnit(1, Team.Player, new HexCoordinate(0, 0));
            BattleUnit target = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0));
            BattleUnit unregistered = CreateUnit(9, Team.Enemy, new HexCoordinate(1, 0));
            board.TryPlace(mover, mover.Coordinate);
            board.TryPlace(target, target.Coordinate);

            bool found = _pathfinder.TryFindPathToAttackPosition(
                board, mover, new[] { null, mover, unregistered, target }, _path, out BattleUnit selected);

            Assert.That(found, Is.True);
            Assert.That(selected, Is.SameAs(target));
            Assert.That(_path.Count, Is.EqualTo(2));
        }

        static BattleUnit CreateUnit(int id, Team team, HexCoordinate coordinate)
        {
            return new BattleUnit(id, team, coordinate, 1, 1f);
        }
    }
}
