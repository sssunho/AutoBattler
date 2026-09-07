using AutoBattler.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleTargetingSystemTests
    {
        [Test]
        public void ValidateCurrentTarget_KeepsInRangeTargetEvenWhenAnotherEnemyIsCloser()
        {
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit unit = PlaceUnit(board, 1, Team.Player, 0, 3);
            BattleUnit target = PlaceUnit(board, 2, Team.Enemy, 3);
            PlaceUnit(board, 9, Team.Enemy, 1);
            unit.Target = target;

            CreateTargetingSystem(board).ValidateCurrentTarget(unit);

            Assert.That(unit.Target, Is.SameAs(target));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ValidateCurrentTarget_ClearsTargetThatCannotBeKept(int invalidation)
        {
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit unit = PlaceUnit(board, 1, Team.Player, 0);
            BattleUnit target = PlaceUnit(board, 2, Team.Enemy, 1);
            unit.Target = target;

            if (invalidation == 0)
            {
                target.IsAlive = false;
            }
            else if (invalidation == 1)
            {
                board.Remove(target);
            }
            else if (invalidation == 2)
            {
                board.TryMove(target, new HexCoordinate(3, 0));
            }
            else
            {
                unit.Target = new BattleUnit(target.Id, target.Team, target.Coordinate, 1, 1f);
            }

            CreateTargetingSystem(board).ValidateCurrentTarget(unit);

            Assert.That(unit.Target, Is.Null);
        }

        [Test]
        public void TryAcquireTargetInRange_PrefersNearestEnemyOverHigherId()
        {
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit unit = PlaceUnit(board, 1, Team.Player, 0, 3);
            PlaceUnit(board, 99, Team.Enemy, 3);
            BattleUnit nearest = PlaceUnit(board, 2, Team.Enemy, 1);

            bool found = CreateTargetingSystem(board).TryAcquireTargetInRange(unit);

            Assert.That(found, Is.True);
            Assert.That(unit.Target, Is.SameAs(nearest));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TryAcquireTargetInRange_PrefersHigherIdForEqualDistance(bool reversePlacement)
        {
            BattleBoard board = new BattleBoard(new HexMap(3, 1));
            BattleUnit unit = PlaceUnit(board, 1, Team.Player, 1);
            BattleUnit highId;

            if (reversePlacement)
            {
                highId = PlaceUnit(board, 9, Team.Enemy, 0);
                PlaceUnit(board, 2, Team.Enemy, 2);
            }
            else
            {
                PlaceUnit(board, 2, Team.Enemy, 2);
                highId = PlaceUnit(board, 9, Team.Enemy, 0);
            }

            bool found = CreateTargetingSystem(board).TryAcquireTargetInRange(unit);

            Assert.That(found, Is.True);
            Assert.That(unit.Target, Is.SameAs(highId));
        }

        [Test]
        public void TryAcquireTargetInRange_IgnoresFriendlyAndDeadUnits()
        {
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit unit = PlaceUnit(board, 1, Team.Player, 0, 3);
            PlaceUnit(board, 9, Team.Player, 1);
            BattleUnit dead = PlaceUnit(board, 8, Team.Enemy, 2);
            dead.IsAlive = false;
            BattleUnit target = PlaceUnit(board, 2, Team.Enemy, 3);

            bool found = CreateTargetingSystem(board).TryAcquireTargetInRange(unit);

            Assert.That(found, Is.True);
            Assert.That(unit.Target, Is.SameAs(target));
        }

        [Test]
        public void TryAcquireReachableTarget_ReplacesOutOfRangeTargetWithCloserEnemy()
        {
            BattleBoard board = new BattleBoard(new HexMap(6, 1));
            BattleUnit unit = PlaceUnit(board, 1, Team.Player, 0);
            BattleUnit previous = PlaceUnit(board, 9, Team.Enemy, 5);
            BattleUnit closer = PlaceUnit(board, 2, Team.Enemy, 2);
            unit.Target = previous;
            BattleTargetingSystem targeting = CreateTargetingSystem(board);

            targeting.ValidateCurrentTarget(unit);
            Assert.That(unit.Target, Is.Null);
            Assert.That(targeting.TryAcquireTargetInRange(unit), Is.False);
            bool found = targeting.TryAcquireReachableTarget(unit, out HexCoordinate nextStep);

            Assert.That(found, Is.True);
            Assert.That(unit.Target, Is.SameAs(closer));
            Assert.That(nextStep, Is.EqualTo(new HexCoordinate(1, 0)));
        }

        [Test]
        public void TryAcquireReachableTarget_DoesNotReuseResultAfterTargetIsRemoved()
        {
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit unit = PlaceUnit(board, 1, Team.Player, 0);
            BattleUnit target = PlaceUnit(board, 2, Team.Enemy, 3);
            BattleTargetingSystem targeting = CreateTargetingSystem(board);
            Assert.That(targeting.TryAcquireReachableTarget(unit, out _), Is.True);
            board.Remove(target);

            bool found = targeting.TryAcquireReachableTarget(unit, out HexCoordinate nextStep);

            Assert.That(found, Is.False);
            Assert.That(unit.Target, Is.Null);
            Assert.That(nextStep, Is.EqualTo(default(HexCoordinate)));
        }

        static BattleTargetingSystem CreateTargetingSystem(BattleBoard board)
        {
            return new BattleTargetingSystem(board, new BattlePathfinder());
        }

        static BattleUnit PlaceUnit(BattleBoard board, int id, Team team, int q, int attackRange = 1)
        {
            BattleUnit unit = new BattleUnit(id, team, new HexCoordinate(q, 0), attackRange, 1f);
            Assert.That(board.TryPlace(unit, unit.Coordinate), Is.True);
            return unit;
        }
    }
}
