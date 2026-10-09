using AutoBattler.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleMovementSystemTests
    {
        [Test]
        public void Tick_MovesUnitsOneCoordinateTowardEachOther()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(5, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(4, 0), 1f);
            PlaceOpponents(board, player, enemy);
            AutoBattlerBattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(3, 0)));
        }

        [Test]
        public void Tick_StopsUnitsWhenTheyReachAttackRange()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(5, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(4, 0), 1f);
            PlaceOpponents(board, player, enemy);
            AutoBattlerBattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);
            movementSystem.Tick(1f);
            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(player.Coordinate.DistanceTo(enemy.Coordinate), Is.EqualTo(1));
        }

        [Test]
        public void Tick_DoesNotMoveUnitWithoutEnoughGauge()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 0.5f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0), 0f);
            PlaceOpponents(board, player, enemy);
            AutoBattlerBattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0.5f));
        }

        [Test]
        public void Tick_DoesNotAccumulateGaugeAfterEarlierEnemyMovesIntoRange()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(5, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(4, 0), 1f);
            PlaceOpponents(board, player, enemy);
            AutoBattlerBattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);
            movementSystem.Tick(1f);

            Assert.That(player.MoveGauge, Is.EqualTo(0f));
            Assert.That(enemy.MoveGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_GivesDestinationPriorityToHigherMoveSpeed()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 2f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0), 1f);
            PlaceOpponents(board, player, enemy);
            AutoBattlerBattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(enemy.MoveGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_GivesDestinationPriorityToHigherUnitIdWhenSpeedsMatch()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0), 1f);
            PlaceOpponents(board, player, enemy);
            AutoBattlerBattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_CapsMoveGaugeWhileUnitCannotMove()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 10f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0), 0f);
            PlaceOpponents(board, player, enemy);
            AutoBattlerBattleUnit blocker = CreateUnit(3, AutoBattlerTeam.Player, new HexCoordinate(1, 0), 0f);
            blocker.AttackPhase = AutoBattlerBattleAttackPhase.Animation;
            board.TryPlace(blocker, blocker.Coordinate);
            AutoBattlerBattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);
            movementSystem.Tick(1f);

            Assert.That(player.MoveGauge, Is.EqualTo(2f));
            Assert.That(player.Target, Is.Null);
            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tick_LaterUnitMovesIntoCoordinateVacatedByFasterUnit(bool reversePlacement)
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(6, 2));
            AutoBattlerBattleUnit leader = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(1, 0), 2f);
            AutoBattlerBattleUnit follower = CreateUnit(9, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(3, AutoBattlerTeam.Enemy, new HexCoordinate(5, 0), 0f);
            enemy.AttackPhase = AutoBattlerBattleAttackPhase.Animation;

            if (reversePlacement)
            {
                board.TryPlace(follower, follower.Coordinate);
                board.TryPlace(leader, leader.Coordinate);
            }
            else
            {
                board.TryPlace(leader, leader.Coordinate);
                board.TryPlace(follower, follower.Coordinate);
            }

            board.TryPlace(enemy, enemy.Coordinate);

            CreateMovementSystem(board).Tick(1f);

            Assert.That(leader.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(follower.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(board.IsOccupied(new HexCoordinate(0, 0)), Is.False);
            Assert.That(board.TryGetUnit(new HexCoordinate(1, 0), out AutoBattlerBattleUnit occupant), Is.True);
            Assert.That(occupant, Is.SameAs(follower));
            Assert.That(leader.MoveGauge, Is.EqualTo(1f));
            Assert.That(follower.MoveGauge, Is.EqualTo(0f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tick_HigherIdMovesFirstRegardlessOfPlacementOrder(bool reversePlacement)
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0), 1f);

            if (reversePlacement)
            {
                board.TryPlace(enemy, enemy.Coordinate);
                board.TryPlace(player, player.Coordinate);
            }
            else
            {
                board.TryPlace(player, player.Coordinate);
                board.TryPlace(enemy, enemy.Coordinate);
            }

            CreateMovementSystem(board).Tick(1f);

            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(player.Target, Is.SameAs(enemy));
            Assert.That(player.MoveGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_AttackingUnitClearsOutOfRangeTargetWithoutAcquiringOrMoving()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0), 0f);
            PlaceOpponents(board, player, enemy);
            player.AttackPhase = AutoBattlerBattleAttackPhase.Cooldown;
            player.MoveGauge = 0.75f;

            CreateMovementSystem(board).Tick(1f);

            Assert.That(player.Target, Is.Null);
            Assert.That(player.IsAttacking, Is.True);
            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0.75f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tick_PreservesGaugeWhenTargetIsInRange(bool hasTarget)
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0), 0f);
            board.TryPlace(player, player.Coordinate);
            board.TryPlace(enemy, enemy.Coordinate);
            player.MoveGauge = 0.75f;

            if (hasTarget)
            {
                player.Target = enemy;
            }

            CreateMovementSystem(board).Tick(1f);

            Assert.That(player.Target, Is.SameAs(enemy));
            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0.75f));
        }

        [Test]
        public void Tick_AcquiresReachableTargetBeforeGaugeIsReady()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 0.5f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0), 0f);
            board.TryPlace(player, player.Coordinate);
            board.TryPlace(enemy, enemy.Coordinate);

            CreateMovementSystem(board).Tick(1f);

            Assert.That(player.Target, Is.SameAs(enemy));
            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0.5f));
        }

        [Test]
        public void Tick_DeadUnitDoesNotAccumulateGaugeOrMove()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0), 0f);
            PlaceOpponents(board, player, enemy);
            player.IsAlive = false;
            player.MoveGauge = 0.5f;

            CreateMovementSystem(board).Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0.5f));
            Assert.That(enemy.Target, Is.Null);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Tick_InvalidDurationDoesNotChangeState(float duration)
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(4, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 1f);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(3, 0), 0f);
            PlaceOpponents(board, player, enemy);
            player.MoveGauge = 0.5f;

            CreateMovementSystem(board).Tick(duration);

            Assert.That(player.Target, Is.SameAs(enemy));
            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0.5f));
        }

        [Test]
        public void Tick_EmptyBoardDoesNotThrow()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(0, 0));

            Assert.DoesNotThrow(() => CreateMovementSystem(board).Tick(1f));
        }

        static AutoBattlerBattleMovementSystem CreateMovementSystem(AutoBattlerBattleBoard board)
        {
            return new AutoBattlerBattleMovementSystem(board, new AutoBattlerBattleTargetingSystem(board, new AutoBattlerBattlePathfinder()));
        }

        static AutoBattlerBattleUnit CreateUnit(
            int id,
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            float moveSpeed)
        {
            return new AutoBattlerBattleUnit(id, team, coordinate, 1, moveSpeed);
        }

        static void PlaceOpponents(
            AutoBattlerBattleBoard board,
            AutoBattlerBattleUnit player,
            AutoBattlerBattleUnit enemy)
        {
            board.TryPlace(player, player.Coordinate);
            board.TryPlace(enemy, enemy.Coordinate);
            player.Target = enemy;
            enemy.Target = player;
        }
    }
}
