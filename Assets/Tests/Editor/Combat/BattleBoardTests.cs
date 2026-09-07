using System;
using AutoBattler.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleBoardTests
    {
        [Test]
        public void Constructor_ThrowsForNullMap()
        {
            Assert.Throws<ArgumentNullException>(() => new BattleBoard(null));
        }

        [Test]
        public void TryPlace_RegistersUnitAndOccupiesCoordinate()
        {
            HexMap map = new HexMap(3, 3);
            BattleBoard board = new BattleBoard(map);
            BattleUnit unit = CreateUnit(1, new HexCoordinate(0, 0));
            HexCoordinate coordinate = new HexCoordinate(2, 1);

            bool placed = board.TryPlace(unit, coordinate);

            Assert.That(placed, Is.True);
            Assert.That(unit.Coordinate, Is.EqualTo(coordinate));
            Assert.That(board.IsOccupied(coordinate), Is.True);
            Assert.That(board.TryGetUnit(unit.Id, out BattleUnit unitById), Is.True);
            Assert.That(unitById, Is.SameAs(unit));
            Assert.That(board.TryGetUnit(coordinate, out BattleUnit unitByCoordinate), Is.True);
            Assert.That(unitByCoordinate, Is.SameAs(unit));
        }

        [Test]
        public void TryPlace_RejectsOccupiedCoordinate()
        {
            BattleBoard board = new BattleBoard(new HexMap(2, 2));
            BattleUnit first = CreateUnit(1, new HexCoordinate(0, 0));
            BattleUnit second = CreateUnit(2, new HexCoordinate(1, 1));
            HexCoordinate coordinate = new HexCoordinate(0, 1);
            board.TryPlace(first, coordinate);

            bool placed = board.TryPlace(second, coordinate);

            Assert.That(placed, Is.False);
            Assert.That(board.TryGetUnit(coordinate, out BattleUnit occupant), Is.True);
            Assert.That(occupant, Is.SameAs(first));
            Assert.That(board.TryGetUnit(second.Id, out _), Is.False);
        }

        [Test]
        public void TryPlace_RejectsDuplicateUnitId()
        {
            BattleBoard board = new BattleBoard(new HexMap(2, 2));
            BattleUnit first = CreateUnit(1, new HexCoordinate(0, 0));
            BattleUnit duplicate = CreateUnit(1, new HexCoordinate(1, 1));
            board.TryPlace(first, new HexCoordinate(0, 0));

            bool placed = board.TryPlace(duplicate, new HexCoordinate(1, 1));

            Assert.That(placed, Is.False);
            Assert.That(board.TryGetUnit(1, out BattleUnit registered), Is.True);
            Assert.That(registered, Is.SameAs(first));
        }

        [Test]
        public void TryMove_UpdatesUnitAndBothCoordinates()
        {
            BattleBoard board = new BattleBoard(new HexMap(3, 3));
            HexCoordinate origin = new HexCoordinate(0, 0);
            HexCoordinate destination = new HexCoordinate(1, 0);
            BattleUnit unit = CreateUnit(1, origin);
            board.TryPlace(unit, origin);

            bool moved = board.TryMove(unit, destination);

            Assert.That(moved, Is.True);
            Assert.That(unit.Coordinate, Is.EqualTo(destination));
            Assert.That(board.IsOccupied(origin), Is.False);
            Assert.That(board.TryGetUnit(destination, out BattleUnit occupant), Is.True);
            Assert.That(occupant, Is.SameAs(unit));
        }

        [Test]
        public void TryMove_RejectsOccupiedDestinationWithoutChangingState()
        {
            BattleBoard board = new BattleBoard(new HexMap(3, 3));
            HexCoordinate origin = new HexCoordinate(0, 0);
            HexCoordinate destination = new HexCoordinate(1, 0);
            BattleUnit movingUnit = CreateUnit(1, origin);
            BattleUnit blockingUnit = CreateUnit(2, destination);
            board.TryPlace(movingUnit, origin);
            board.TryPlace(blockingUnit, destination);

            bool moved = board.TryMove(movingUnit, destination);

            Assert.That(moved, Is.False);
            Assert.That(movingUnit.Coordinate, Is.EqualTo(origin));
            Assert.That(board.TryGetUnit(origin, out BattleUnit originOccupant), Is.True);
            Assert.That(originOccupant, Is.SameAs(movingUnit));
            Assert.That(board.TryGetUnit(destination, out BattleUnit destinationOccupant), Is.True);
            Assert.That(destinationOccupant, Is.SameAs(blockingUnit));
        }

        [Test]
        public void TryMove_RejectsDifferentInstanceWithRegisteredId()
        {
            BattleBoard board = new BattleBoard(new HexMap(3, 3));
            BattleUnit registered = CreateUnit(1, new HexCoordinate(0, 0));
            BattleUnit impostor = CreateUnit(1, new HexCoordinate(0, 0));
            board.TryPlace(registered, registered.Coordinate);

            bool moved = board.TryMove(impostor, new HexCoordinate(1, 0));

            Assert.That(moved, Is.False);
            Assert.That(registered.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
        }

        [Test]
        public void Remove_ReleasesCoordinateAndRegistration()
        {
            BattleBoard board = new BattleBoard(new HexMap(2, 2));
            HexCoordinate coordinate = new HexCoordinate(1, 1);
            BattleUnit unit = CreateUnit(1, coordinate);
            board.TryPlace(unit, coordinate);

            bool removed = board.Remove(unit);

            Assert.That(removed, Is.True);
            Assert.That(board.IsOccupied(coordinate), Is.False);
            Assert.That(board.TryGetUnit(unit.Id, out _), Is.False);
            Assert.That(board.TryGetUnit(coordinate, out _), Is.False);
        }

        [Test]
        public void TryApplyDamage_ReducesCurrentHealthWithoutRemovingLivingUnit()
        {
            BattleBoard board = new BattleBoard(new HexMap(2, 1));
            BattleUnit unit = new BattleUnit(
                1,
                Team.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                100,
                30);
            board.TryPlace(unit, unit.Coordinate);

            bool applied = board.TryApplyDamage(unit, 40);

            Assert.That(applied, Is.True);
            Assert.That(unit.CurrentHealth, Is.EqualTo(60));
            Assert.That(unit.IsAlive, Is.True);
            Assert.That(board.TryGetUnit(unit.Id, out BattleUnit registered), Is.True);
            Assert.That(registered, Is.SameAs(unit));
        }

        [Test]
        public void TryApplyDamage_ClampsHealthToZeroAndRemovesDeadUnit()
        {
            BattleBoard board = new BattleBoard(new HexMap(2, 1));
            BattleUnit unit = new BattleUnit(
                1,
                Team.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                50,
                30);
            board.TryPlace(unit, unit.Coordinate);
            unit.AttackPhase = BattleAttackPhase.Animation;
            unit.AttackTarget = unit;
            unit.AttackElapsed = 0.5f;

            bool applied = board.TryApplyDamage(unit, 80);

            Assert.That(applied, Is.True);
            Assert.That(unit.CurrentHealth, Is.Zero);
            Assert.That(unit.IsAlive, Is.False);
            Assert.That(unit.IsAttacking, Is.False);
            Assert.That(unit.AttackPhase, Is.EqualTo(BattleAttackPhase.None));
            Assert.That(unit.AttackTarget, Is.Null);
            Assert.That(unit.AttackElapsed, Is.Zero);
            Assert.That(unit.Target, Is.Null);
            Assert.That(board.IsOccupied(unit.Coordinate), Is.False);
            Assert.That(board.TryGetUnit(unit.Id, out _), Is.False);
        }

        [Test]
        public void TryApplyDamage_RejectsNegativeDamageWithoutChangingState()
        {
            BattleBoard board = new BattleBoard(new HexMap(2, 1));
            BattleUnit unit = new BattleUnit(
                1,
                Team.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                50,
                30);
            board.TryPlace(unit, unit.Coordinate);

            bool applied = board.TryApplyDamage(unit, -1);

            Assert.That(applied, Is.False);
            Assert.That(unit.CurrentHealth, Is.EqualTo(50));
            Assert.That(unit.IsAlive, Is.True);
        }

        static BattleUnit CreateUnit(int id, HexCoordinate coordinate)
        {
            return new BattleUnit(id, Team.Player, coordinate, 1, 1f);
        }
    }
}
