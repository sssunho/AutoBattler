using AutoBattler.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleSimulationTests
    {
        [Test]
        public void Constructor_CreatesBoardWithRequestedSize()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(4, 3);

            Assert.That(simulation.Board.Width, Is.EqualTo(4));
            Assert.That(simulation.Board.Height, Is.EqualTo(3));
        }

        [Test]
        public void TrySpawnUnit_CreatesAndPlacesUnit()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(3, 2);
            HexCoordinate coordinate = new HexCoordinate(2, 1);

            bool spawned = simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                coordinate,
                1,
                1f,
                out AutoBattlerBattleUnit unit);

            Assert.That(spawned, Is.True);
            Assert.That(unit, Is.Not.Null);
            Assert.That(unit.Id, Is.EqualTo(1));
            Assert.That(unit.Team, Is.EqualTo(AutoBattlerTeam.Player));
            Assert.That(unit.Coordinate, Is.EqualTo(coordinate));
            Assert.That(simulation.Board.TryGetUnit(coordinate, out AutoBattlerBattleUnit occupant), Is.True);
            Assert.That(occupant, Is.SameAs(unit));
        }

        [Test]
        public void TrySpawnUnit_InitializesCombatStats()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(2, 1);

            bool spawned = simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                250,
                35,
                out AutoBattlerBattleUnit unit);

            Assert.That(spawned, Is.True);
            Assert.That(unit.MaxHealth, Is.EqualTo(250));
            Assert.That(unit.CurrentHealth, Is.EqualTo(250));
            Assert.That(unit.AttackDamage, Is.EqualTo(35));
            Assert.That(unit.AttackAnimationDuration, Is.EqualTo(1.6f));
            Assert.That(unit.AttackHitNormalizedTime, Is.EqualTo(0.4f));
            Assert.That(unit.AttackCooldownDuration, Is.EqualTo(0.4f));
            Assert.That(unit.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.None));
            Assert.That(unit.AttackTarget, Is.Null);
            Assert.That(unit.AttackElapsed, Is.Zero);
            Assert.That(unit.IsAttacking, Is.False);
        }

        [Test]
        public void TryApplyBasicAttackDamage_UsesAttackDamageAndRemovesKilledTarget()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(2, 1);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                100,
                40,
                out AutoBattlerBattleUnit attacker);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Enemy,
                new HexCoordinate(1, 0),
                1,
                1f,
                30,
                10,
                out AutoBattlerBattleUnit target);

            bool applied = simulation.TryApplyBasicAttackDamage(attacker, target);

            Assert.That(applied, Is.True);
            Assert.That(target.CurrentHealth, Is.Zero);
            Assert.That(target.IsAlive, Is.False);
            Assert.That(simulation.Board.TryGetUnit(target.Id, out _), Is.False);
            Assert.That(simulation.Board.IsOccupied(target.Coordinate), Is.False);
        }

        [Test]
        public void TrySpawnUnit_AssignsSequentialIdsToSuccessfulSpawns()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(3, 1);

            simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                out AutoBattlerBattleUnit first);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Enemy,
                new HexCoordinate(2, 0),
                1,
                1f,
                out AutoBattlerBattleUnit second);

            Assert.That(first.Id, Is.EqualTo(1));
            Assert.That(second.Id, Is.EqualTo(2));
        }

        [Test]
        public void TrySpawnUnit_DoesNotConsumeIdWhenPlacementFails()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(2, 1);
            HexCoordinate occupiedCoordinate = new HexCoordinate(0, 0);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                occupiedCoordinate,
                1,
                1f,
                out AutoBattlerBattleUnit first);

            bool failed = simulation.TrySpawnUnit(
                AutoBattlerTeam.Enemy,
                occupiedCoordinate,
                1,
                1f,
                out AutoBattlerBattleUnit rejected);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Enemy,
                new HexCoordinate(1, 0),
                1,
                1f,
                out AutoBattlerBattleUnit second);

            Assert.That(failed, Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(first.Id, Is.EqualTo(1));
            Assert.That(second.Id, Is.EqualTo(2));
        }

        [Test]
        public void TrySpawnUnit_RejectsInvalidStats()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(2, 1);

            bool invalidRange = simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                -1,
                1f,
                out AutoBattlerBattleUnit rangeUnit);
            bool invalidSpeed = simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                float.NaN,
                out AutoBattlerBattleUnit speedUnit);
            bool invalidHealth = simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                0,
                10,
                out AutoBattlerBattleUnit healthUnit);
            bool invalidDamage = simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                100,
                -1,
                out AutoBattlerBattleUnit damageUnit);

            Assert.That(invalidRange, Is.False);
            Assert.That(invalidSpeed, Is.False);
            Assert.That(invalidHealth, Is.False);
            Assert.That(invalidDamage, Is.False);
            Assert.That(rangeUnit, Is.Null);
            Assert.That(speedUnit, Is.Null);
            Assert.That(healthUnit, Is.Null);
            Assert.That(damageUnit, Is.Null);
        }

        [Test]
        public void TrySetTarget_ConnectsRegisteredOpponents()
        {
            AutoBattlerBattleSimulation simulation = CreateDuel(out AutoBattlerBattleUnit player, out AutoBattlerBattleUnit enemy);

            bool playerTargeted = simulation.TrySetTarget(player, enemy);
            bool enemyTargeted = simulation.TrySetTarget(enemy, player);

            Assert.That(playerTargeted, Is.True);
            Assert.That(enemyTargeted, Is.True);
            Assert.That(player.Target, Is.SameAs(enemy));
            Assert.That(enemy.Target, Is.SameAs(player));
        }

        [Test]
        public void TrySetTarget_RejectsFriendlyTarget()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(2, 1);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                out AutoBattlerBattleUnit first);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(1, 0),
                1,
                1f,
                out AutoBattlerBattleUnit second);

            bool targeted = simulation.TrySetTarget(first, second);

            Assert.That(targeted, Is.False);
            Assert.That(first.Target, Is.Null);
        }

        [Test]
        public void TickMovement_AcquiresTargetsAndMovesOpponentsTowardAttackRange()
        {
            AutoBattlerBattleSimulation simulation = CreateDuel(out AutoBattlerBattleUnit player, out AutoBattlerBattleUnit enemy);

            Assert.That(player.Target, Is.Null);
            Assert.That(enemy.Target, Is.Null);

            simulation.TickMovement(1f);
            simulation.TickMovement(1f);

            Assert.That(player.Coordinate.DistanceTo(enemy.Coordinate), Is.EqualTo(1));
            Assert.That(player.Target, Is.SameAs(enemy));
            Assert.That(enemy.Target, Is.SameAs(player));
        }

        [Test]
        public void TickAttacks_StartsImmediatelyAfterMovementEntersRange()
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(3, 1);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                out AutoBattlerBattleUnit player);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Enemy,
                new HexCoordinate(2, 0),
                1,
                1f,
                out AutoBattlerBattleUnit enemy);

            simulation.TickMovement(1f);
            simulation.TickAttacks(0.01f);

            Assert.That(player.Coordinate.DistanceTo(enemy.Coordinate), Is.EqualTo(1));
            Assert.That(player.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.Animation));
            Assert.That(enemy.AttackPhase, Is.EqualTo(AutoBattlerBattleAttackPhase.Animation));
        }

        static AutoBattlerBattleSimulation CreateDuel(
            out AutoBattlerBattleUnit player,
            out AutoBattlerBattleUnit enemy)
        {
            AutoBattlerBattleSimulation simulation = new AutoBattlerBattleSimulation(5, 1);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                out player);
            simulation.TrySpawnUnit(
                AutoBattlerTeam.Enemy,
                new HexCoordinate(4, 0),
                1,
                1f,
                out enemy);
            return simulation;
        }
    }
}
