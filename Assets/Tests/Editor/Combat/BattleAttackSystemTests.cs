using RaggedBanner.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleAttackSystemTests
    {
        [Test]
        public void Tick_StartsAttackAndWaitsForNormalizedHitTime()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit attacker = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 100, 25);
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0), 100, 10);
            PlaceOpponents(board, attacker, target);

            new AutoBattlerBattleAttackSystem(board).Tick(0.63f);

            Assert.That(attacker.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.Animation));
            Assert.That(attacker.AttackTarget, Is.SameAs(target));
            Assert.That(attacker.AttackElapsed, Is.EqualTo(0.63f));
            Assert.That(target.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Tick_AppliesDamageOnceWhenAnimationCrossesHitTime()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit attacker = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 100, 25);
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0), 100, 10);
            PlaceOpponents(board, attacker, target);
            AutoBattlerBattleAttackSystem attackSystem = new AutoBattlerBattleAttackSystem(board);

            attackSystem.Tick(0.63f);
            attackSystem.Tick(0.02f);
            attackSystem.Tick(0.5f);

            Assert.That(target.CurrentHealth, Is.EqualTo(75));
        }

        [Test]
        public void Tick_CarriesRemainingTimeIntoCooldownAndEndsAfterTotalDuration()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit attacker = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 100, 10);
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0), 100, 10);
            PlaceOpponents(board, attacker, target);
            AutoBattlerBattleAttackSystem attackSystem = new AutoBattlerBattleAttackSystem(board);

            attackSystem.Tick(1.7f);

            Assert.That(attacker.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.Cooldown));
            Assert.That(attacker.AttackElapsed, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(attacker.AttackTarget, Is.SameAs(target));

            attackSystem.Tick(0.31f);

            Assert.That(attacker.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.None));
            Assert.That(attacker.AttackElapsed, Is.Zero);
            Assert.That(attacker.AttackTarget, Is.Null);
        }

        [Test]
        public void Tick_KeepsAttackTargetWhenCurrentTargetChanges()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit attacker = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(1, 0), 100, 25);
            AutoBattlerBattleUnit firstTarget = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(0, 0), 100, 10);
            AutoBattlerBattleUnit secondTarget = CreateUnit(3, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0), 100, 10);
            board.TryPlace(attacker, attacker.Coordinate);
            board.TryPlace(firstTarget, firstTarget.Coordinate);
            board.TryPlace(secondTarget, secondTarget.Coordinate);
            attacker.Target = firstTarget;
            AutoBattlerBattleAttackSystem attackSystem = new AutoBattlerBattleAttackSystem(board);

            attackSystem.Tick(0.1f);
            attacker.Target = secondTarget;
            attackSystem.Tick(0.55f);

            Assert.That(attacker.AttackTarget, Is.SameAs(firstTarget));
            Assert.That(firstTarget.CurrentHealth, Is.EqualTo(75));
            Assert.That(secondTarget.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Tick_CompletesAttackWithoutDamageWhenAttackTargetWasRemoved()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit attacker = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 100, 25);
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0), 100, 10);
            PlaceOpponents(board, attacker, target);
            AutoBattlerBattleAttackSystem attackSystem = new AutoBattlerBattleAttackSystem(board);

            attackSystem.Tick(0.1f);
            board.Remove(target);
            attackSystem.Tick(1.91f);

            Assert.That(target.CurrentHealth, Is.EqualTo(100));
            Assert.That(attacker.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.None));
            Assert.That(attacker.AttackTarget, Is.Null);
        }

        [Test]
        public void Tick_ResolvesHitsReachedInSameFrameBeforeDeathsCancelThem()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit player = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 10, 10);
            AutoBattlerBattleUnit enemy = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0), 10, 10);
            PlaceOpponents(board, player, enemy);

            new AutoBattlerBattleAttackSystem(board).Tick(0.65f);

            Assert.That(player.IsAlive, Is.False);
            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(board.TryGetUnit(player.Id, out _), Is.False);
            Assert.That(board.TryGetUnit(enemy.Id, out _), Is.False);
        }

        [Test]
        public void Tick_DoesNotStartAttackAgainstOutOfRangeTarget()
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(3, 1));
            AutoBattlerBattleUnit attacker = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 100, 25);
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(2, 0), 100, 10);
            PlaceOpponents(board, attacker, target);

            new AutoBattlerBattleAttackSystem(board).Tick(1f);

            Assert.That(attacker.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.None));
            Assert.That(target.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Tick_InvalidDurationDoesNotChangeState(float deltaTime)
        {
            AutoBattlerBattleBoard board = new AutoBattlerBattleBoard(new HexMap(2, 1));
            AutoBattlerBattleUnit attacker = CreateUnit(1, AutoBattlerTeam.Player, new HexCoordinate(0, 0), 100, 25);
            AutoBattlerBattleUnit target = CreateUnit(2, AutoBattlerTeam.Enemy, new HexCoordinate(1, 0), 100, 10);
            PlaceOpponents(board, attacker, target);

            new AutoBattlerBattleAttackSystem(board).Tick(deltaTime);

            Assert.That(attacker.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.None));
            Assert.That(target.CurrentHealth, Is.EqualTo(100));
        }

        static AutoBattlerBattleUnit CreateUnit(
            int id,
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            int maxHealth,
            int attackDamage)
        {
            return new AutoBattlerBattleUnit(
                id,
                team,
                coordinate,
                1,
                1f,
                maxHealth,
                attackDamage,
                1.6f,
                0.4f,
                0.4f);
        }

        static void PlaceOpponents(
            AutoBattlerBattleBoard board,
            AutoBattlerBattleUnit first,
            AutoBattlerBattleUnit second)
        {
            board.TryPlace(first, first.Coordinate);
            board.TryPlace(second, second.Coordinate);
            first.Target = second;
            second.Target = first;
        }
    }
}
