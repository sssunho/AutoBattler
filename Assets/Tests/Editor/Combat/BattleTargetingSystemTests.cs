using RaggedBanner.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleTargetingSystemTests
    {
        [Test]
        public void ValidateCurrentTarget_KeepsInRangeTargetEvenWhenAnotherEnemyIsCloser()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit unit = PlaceUnit(board, 1, AutoBattlerTeam.Player, 0, 3);
            AutoBattlerBattleUnit target = PlaceUnit(board, 2, AutoBattlerTeam.Enemy, 3);
            PlaceUnit(board, 9, AutoBattlerTeam.Enemy, 1);
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
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit unit = PlaceUnit(board, 1, AutoBattlerTeam.Player, 0);
            AutoBattlerBattleUnit target = PlaceUnit(board, 2, AutoBattlerTeam.Enemy, 1);
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
                unit.Target = new AutoBattlerBattleUnit(target.Id, target.Team, target.Coordinate, 1, 1f);
            }

            CreateTargetingSystem(board).ValidateCurrentTarget(unit);

            Assert.That(unit.Target, Is.Null);
        }

        [Test]
        public void TryAcquireTargetInRange_PrefersNearestEnemyOverHigherId()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit unit = PlaceUnit(board, 1, AutoBattlerTeam.Player, 0, 3);
            PlaceUnit(board, 99, AutoBattlerTeam.Enemy, 3);
            AutoBattlerBattleUnit nearest = PlaceUnit(board, 2, AutoBattlerTeam.Enemy, 1);

            bool found = CreateTargetingSystem(board).TryAcquireTargetInRange(unit);

            Assert.That(found, Is.True);
            Assert.That(unit.Target, Is.SameAs(nearest));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TryAcquireTargetInRange_PrefersHigherIdForEqualDistance(bool reversePlacement)
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit unit = PlaceUnit(board, 1, AutoBattlerTeam.Player, 1);
            AutoBattlerBattleUnit highId;

            if (reversePlacement)
            {
                highId = PlaceUnit(board, 9, AutoBattlerTeam.Enemy, 0);
                PlaceUnit(board, 2, AutoBattlerTeam.Enemy, 2);
            }
            else
            {
                PlaceUnit(board, 2, AutoBattlerTeam.Enemy, 2);
                highId = PlaceUnit(board, 9, AutoBattlerTeam.Enemy, 0);
            }

            bool found = CreateTargetingSystem(board).TryAcquireTargetInRange(unit);

            Assert.That(found, Is.True);
            Assert.That(unit.Target, Is.SameAs(highId));
        }

        [Test]
        public void TryAcquireTargetInRange_IgnoresFriendlyAndDeadUnits()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit unit = PlaceUnit(board, 1, AutoBattlerTeam.Player, 0, 3);
            PlaceUnit(board, 9, AutoBattlerTeam.Player, 1);
            AutoBattlerBattleUnit dead = PlaceUnit(board, 8, AutoBattlerTeam.Enemy, 2);
            dead.IsAlive = false;
            AutoBattlerBattleUnit target = PlaceUnit(board, 2, AutoBattlerTeam.Enemy, 3);

            bool found = CreateTargetingSystem(board).TryAcquireTargetInRange(unit);

            Assert.That(found, Is.True);
            Assert.That(unit.Target, Is.SameAs(target));
        }

        [Test]
        public void TryAcquireReachableTarget_ReplacesOutOfRangeTargetWithCloserEnemy()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(6, 1));
            AutoBattlerBattleUnit unit = PlaceUnit(board, 1, AutoBattlerTeam.Player, 0);
            AutoBattlerBattleUnit previous = PlaceUnit(board, 9, AutoBattlerTeam.Enemy, 5);
            AutoBattlerBattleUnit closer = PlaceUnit(board, 2, AutoBattlerTeam.Enemy, 2);
            unit.Target = previous;
            AutoBattlerBattleTargetingSystem targeting = CreateTargetingSystem(board);

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
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit unit = PlaceUnit(board, 1, AutoBattlerTeam.Player, 0);
            AutoBattlerBattleUnit target = PlaceUnit(board, 2, AutoBattlerTeam.Enemy, 3);
            AutoBattlerBattleTargetingSystem targeting = CreateTargetingSystem(board);
            Assert.That(targeting.TryAcquireReachableTarget(unit, out _), Is.True);
            board.Remove(target);

            bool found = targeting.TryAcquireReachableTarget(unit, out HexCoordinate nextStep);

            Assert.That(found, Is.False);
            Assert.That(unit.Target, Is.Null);
            Assert.That(nextStep, Is.EqualTo(default(HexCoordinate)));
        }

        static AutoBattlerBattleTargetingSystem CreateTargetingSystem(AutoBattlerBattleBoard board)
        {
            return new AutoBattlerBattleTargetingSystem(board, new AutoBattlerBattlePathfinder());
        }

        static AutoBattlerBattleUnit PlaceUnit(AutoBattlerBattleBoard board, int id, AutoBattlerTeam team, int q, int attackRange = 1)
        {
            AutoBattlerBattleUnit unit = new AutoBattlerBattleUnit(id, team, new HexCoordinate(q, 0), attackRange, 1f);
            Assert.That(board.TryPlace(unit, unit.Coordinate), Is.True);
            return unit;
        }
    }
}
