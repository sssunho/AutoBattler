using AutoBattler.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleMovementSystemTests
    {
        [Test]
        public void Tick_MovesUnitsOneCoordinateTowardEachOther()
        {
            BattleBoard board = new BattleBoard(new HexMap(5, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(4, 0), 1f);
            PlaceOpponents(board, player, enemy);
            BattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(3, 0)));
        }

        [Test]
        public void Tick_StopsUnitsWhenTheyReachAttackRange()
        {
            BattleBoard board = new BattleBoard(new HexMap(5, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(4, 0), 1f);
            PlaceOpponents(board, player, enemy);
            BattleMovementSystem movementSystem = CreateMovementSystem(board);

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
            BattleBoard board = new BattleBoard(new HexMap(3, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 0.5f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(2, 0), 0f);
            PlaceOpponents(board, player, enemy);
            BattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0.5f));
        }

        [Test]
        public void Tick_DoesNotAccumulateGaugeAfterEarlierEnemyMovesIntoRange()
        {
            BattleBoard board = new BattleBoard(new HexMap(5, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(4, 0), 1f);
            PlaceOpponents(board, player, enemy);
            BattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);
            movementSystem.Tick(1f);

            Assert.That(player.MoveGauge, Is.EqualTo(0f));
            Assert.That(enemy.MoveGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_GivesDestinationPriorityToHigherMoveSpeed()
        {
            BattleBoard board = new BattleBoard(new HexMap(3, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 2f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(2, 0), 1f);
            PlaceOpponents(board, player, enemy);
            BattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(enemy.MoveGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_GivesDestinationPriorityToHigherUnitIdWhenSpeedsMatch()
        {
            BattleBoard board = new BattleBoard(new HexMap(3, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(2, 0), 1f);
            PlaceOpponents(board, player, enemy);
            BattleMovementSystem movementSystem = CreateMovementSystem(board);

            movementSystem.Tick(1f);

            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(player.MoveGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_CapsMoveGaugeWhileUnitCannotMove()
        {
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 10f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0), 0f);
            PlaceOpponents(board, player, enemy);
            BattleUnit blocker = CreateUnit(3, Team.Player, new HexCoordinate(1, 0), 0f);
            blocker.AttackPhase = BattleAttackPhase.Animation;
            board.TryPlace(blocker, blocker.Coordinate);
            BattleMovementSystem movementSystem = CreateMovementSystem(board);

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
            BattleBoard board = new BattleBoard(new HexMap(6, 2));
            BattleUnit leader = CreateUnit(1, Team.Player, new HexCoordinate(1, 0), 2f);
            BattleUnit follower = CreateUnit(9, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(3, Team.Enemy, new HexCoordinate(5, 0), 0f);
            enemy.AttackPhase = BattleAttackPhase.Animation;

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
            Assert.That(board.TryGetUnit(new HexCoordinate(1, 0), out BattleUnit occupant), Is.True);
            Assert.That(occupant, Is.SameAs(follower));
            Assert.That(leader.MoveGauge, Is.EqualTo(1f));
            Assert.That(follower.MoveGauge, Is.EqualTo(0f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Tick_HigherIdMovesFirstRegardlessOfPlacementOrder(bool reversePlacement)
        {
            BattleBoard board = new BattleBoard(new HexMap(3, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(2, 0), 1f);

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
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0), 0f);
            PlaceOpponents(board, player, enemy);
            player.AttackPhase = BattleAttackPhase.Cooldown;
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
            BattleBoard board = new BattleBoard(new HexMap(2, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(1, 0), 0f);
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
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 0.5f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0), 0f);
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
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0), 0f);
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
            BattleBoard board = new BattleBoard(new HexMap(4, 1));
            BattleUnit player = CreateUnit(1, Team.Player, new HexCoordinate(0, 0), 1f);
            BattleUnit enemy = CreateUnit(2, Team.Enemy, new HexCoordinate(3, 0), 0f);
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
            BattleBoard board = new BattleBoard(new HexMap(0, 0));

            Assert.DoesNotThrow(() => CreateMovementSystem(board).Tick(1f));
        }

        static BattleMovementSystem CreateMovementSystem(BattleBoard board)
        {
            return new BattleMovementSystem(board, new BattleTargetingSystem(board, new BattlePathfinder()));
        }

        static BattleUnit CreateUnit(
            int id,
            Team team,
            HexCoordinate coordinate,
            float moveSpeed)
        {
            return new BattleUnit(id, team, coordinate, 1, moveSpeed);
        }

        static void PlaceOpponents(
            BattleBoard board,
            BattleUnit player,
            BattleUnit enemy)
        {
            board.TryPlace(player, player.Coordinate);
            board.TryPlace(enemy, enemy.Coordinate);
            player.Target = enemy;
            enemy.Target = player;
        }
    }
}
