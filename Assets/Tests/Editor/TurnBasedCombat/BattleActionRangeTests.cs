using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleActionRangeTests
    {
        readonly List<HexCoordinate> _movement = new List<HexCoordinate>();
        readonly List<BattleAttackTarget> _targets = new List<BattleAttackTarget>();

        [TestCase(0, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        public void QueriedDestinationsMatchRealMoveAcceptance(int movement, int range)
        {
            BattleSimulation simulation = CreateLayout(movement, range, 4);
            Assert.That(simulation.TryGetActionRange(1, _movement, _targets), Is.True);
            Assert.That(_movement, Has.No.Member(new HexCoordinate(0, 1)));

            for (int r = -1; r <= simulation.Board.Height; r++)
            {
                for (int q = -1; q <= simulation.Board.Width; q++)
                {
                    HexCoordinate destination = new HexCoordinate(q, r);
                    BattleSimulation command = CreateLayout(movement, range, 4);
                    Assert.That(command.TryMove(1, destination), Is.EqualTo(_movement.Contains(destination)),
                        destination.ToString());
                }
            }
        }

        [TestCase(0, 1, 4)]
        [TestCase(0, 2, 4)]
        [TestCase(3, 2, 4)]
        [TestCase(3, 2, 0)]
        public void QueriedTargetsAndApproachKindsMatchRealAttackAcceptance(int movement, int range, int ap)
        {
            BattleSimulation simulation = CreateLayout(movement, range, ap);
            Assert.That(simulation.TryGetActionRange(1, _movement, _targets), Is.True);

            for (int targetId = 0; targetId <= 7; targetId++)
            {
                BattleSimulation command = CreateLayout(movement, range, ap);
                bool found = TryFindTarget(targetId, out BattleAttackTarget target);
                Assert.That(command.TryAttack(1, targetId), Is.EqualTo(found), targetId.ToString());

                if (found)
                {
                    Assert.That(command.Phase == BattleActionPhase.Moving, Is.EqualTo(target.RequiresMovement));
                }
            }
        }

        [Test]
        public void OccupiedCorridorBlocksApproachDespiteEnoughGeometricDistance()
        {
            BattleSimulation simulation = new BattleSimulation(5, 1, randomSeed: 3);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0,
                BattleTestUtility.CreateStats(movementPerTurn: 6));
            Spawn(simulation, BattleTeam.Player, 1, 0);
            Spawn(simulation, BattleTeam.Enemy, 4, 0);
            BattleTestUtility.StartTurn(simulation, player.Id);

            Assert.That(simulation.TryGetActionRange(player.Id, _movement, _targets), Is.True);
            Assert.That(_movement, Is.Empty);
            Assert.That(_targets, Is.Empty);
            Assert.That(simulation.TryAttack(player.Id, 3), Is.False);
        }

        [Test]
        public void OccupiedNeighborsRequireTheActualDetourCost()
        {
            BattleSimulation simulation = CreateLayout(2, 1, 4);
            Assert.That(simulation.TryGetActionRange(1, _movement, _targets), Is.True);
            Assert.That(_movement, Has.No.Member(new HexCoordinate(2, 1)));
            Assert.That(_movement, Does.Contain(new HexCoordinate(1, 2)));

            simulation = CreateLayout(3, 1, 4);
            Assert.That(simulation.TryGetActionRange(1, _movement, _targets), Is.True);
            Assert.That(_movement, Does.Contain(new HexCoordinate(2, 1)));
        }

        [Test]
        public void EmptyMovementStillAllowsDirectAttackAndMissingApOnlyRemovesTargets()
        {
            BattleSimulation simulation = CreateLayout(0, 2, 4);
            Assert.That(simulation.TryGetActionRange(1, _movement, _targets), Is.True);
            Assert.That(_movement, Is.Empty);
            Assert.That(TryFindTarget(6, out BattleAttackTarget direct), Is.True);
            Assert.That(direct.RequiresMovement, Is.False);
            Assert.That(TryFindTarget(4, out _), Is.False);

            simulation = CreateLayout(3, 2, 0);
            Assert.That(simulation.TryGetActionRange(1, _movement, _targets), Is.True);
            Assert.That(_movement, Is.Not.Empty);
            Assert.That(_targets, Is.Empty);
        }

        [Test]
        public void QueryRejectsPreparingOtherTurnsWaitingAndCompletedAndClearsOldResults()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 2);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 3, 3);
            SeedOldResults();
            Assert.That(simulation.TryGetActionRange(player.Id, _movement, _targets), Is.False);
            AssertEmpty();
            BattleTestUtility.StartTurn(simulation, player.Id);
            SeedOldResults();
            Assert.That(simulation.TryGetActionRange(enemy.Id, _movement, _targets), Is.False);
            AssertEmpty();
            Assert.That(simulation.TryGetActionRange(player.Id, _movement, _targets), Is.True);
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);
            Assert.That(simulation.TryGetActionRange(player.Id, _movement, _targets), Is.False);
            AssertEmpty();
            Assert.That(simulation.TryFinalizeResult(out _), Is.True);
            SeedOldResults();
            Assert.That(simulation.TryGetActionRange(player.Id, _movement, _targets), Is.False);
            AssertEmpty();
        }

        [Test]
        public void BusyQueriesDoNotReplaceQueuedOrIssuedMovement()
        {
            BattleSimulation simulation = new BattleSimulation(5, 2, randomSeed: 2);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0,
                BattleTestUtility.CreateStats(movementPerTurn: 3));
            Spawn(simulation, BattleTeam.Enemy, 4, 1);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(2, 0)), Is.True);
            SeedOldResults();
            Assert.That(simulation.TryGetActionRange(player.Id, _movement, _targets), Is.False);
            AssertEmpty();
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep first), Is.True);
            Assert.That(first.To, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(simulation.TryGetActionRange(player.Id, _movement, _targets), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep second), Is.True);
            Assert.That(second.To, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(player.MovementRemaining, Is.EqualTo(1));
            Assert.That(simulation.IsBusy, Is.False);
        }

        [Test]
        public void QueriesPreserveResourcesFacingOccupancyAndCombatRandomSequence()
        {
            BattleSimulation queried = CreateRandomBattle();
            BattleSimulation control = CreateRandomBattle();
            Assert.That(queried.Board.TryGetUnit(1, out HexCharacter player), Is.True);

            for (int attempt = 0; attempt < 12; attempt++)
            {
                int ap = player.ActionPoints;
                int movement = player.MovementRemaining;
                HexCoordinate coordinate = player.Coordinate;
                HexDirection facing = player.Facing;
                uint version = queried.Board.OccupancyVersion;

                for (int i = 0; i < 5; i++)
                {
                    Assert.That(queried.TryGetActionRange(player.Id, _movement, _targets), Is.True);
                }

                Assert.That(player.ActionPoints, Is.EqualTo(ap));
                Assert.That(player.MovementRemaining, Is.EqualTo(movement));
                Assert.That(player.Coordinate, Is.EqualTo(coordinate));
                Assert.That(player.Facing, Is.EqualTo(facing));
                Assert.That(queried.Board.OccupancyVersion, Is.EqualTo(version));
                Assert.That(queried.IsBusy, Is.False);
                BattleAttackResult actual = Attack(queried, 1, 2);
                BattleAttackResult expected = Attack(control, 1, 2);
                Assert.That(actual.Damage, Is.EqualTo(expected.Damage));
                Assert.That(actual.IsCritical, Is.EqualTo(expected.IsCritical));
                Assert.That(actual.WasNegated, Is.EqualTo(expected.WasNegated));
                Assert.That(actual.RemainingHealth, Is.EqualTo(expected.RemainingHealth));
                Assert.That(queried.TryEndTurn(1), Is.True);
                Assert.That(control.TryEndTurn(1), Is.True);
                BattleTestUtility.AdvanceToTurn(queried, 1);
                BattleTestUtility.AdvanceToTurn(control, 1);
            }
        }

        [Test]
        public void OccupancyVersionChangesOnlyWhenOccupancyChanges()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2, randomSeed: 2);
            Assert.That(simulation.Board.OccupancyVersion, Is.Zero);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 2, 0);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(2u));
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, enemy.Coordinate,
                BattleTestUtility.CreateStats(), HexDirection.West, out _), Is.False);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(2u));
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryMove(player.Id, enemy.Coordinate), Is.False);
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(1, 0)), Is.True);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(2u));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(3u));
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryGetActionRange(player.Id, _movement, _targets), Is.True);
            Attack(simulation, player.Id, enemy.Id);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(3u));
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(4u));
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.False);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(4u));
        }

        [Test]
        public void LethalAttackInvalidatesOccupancyAndNullOutputRejectsWithoutChangingState()
        {
            BattleSimulation simulation = new BattleSimulation(3, 2, randomSeed: 2);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 0,
                BattleTestUtility.CreateStats(maxHealth: 1));
            BattleTestUtility.StartTurn(simulation, player.Id);
            SeedOldResults();
            Assert.That(simulation.TryGetActionRange(player.Id, null, _targets), Is.False);
            Assert.That(_targets, Is.Empty);
            Assert.That(simulation.TryGetActionRange(player.Id, _movement, null), Is.False);
            Assert.That(_movement, Is.Empty);
            uint beforeAttack = simulation.Board.OccupancyVersion;
            Assert.That(Attack(simulation, player.Id, enemy.Id).TargetDied, Is.True);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(beforeAttack + 1));
        }

        static BattleSimulation CreateLayout(int movement, int range, int ap)
        {
            BattleRules rules = new BattleRules(startingActionPoints: ap);
            BattleSimulation simulation = new BattleSimulation(5, 4, rules, 23);
            Spawn(simulation, BattleTeam.Player, 0, 1,
                BattleTestUtility.CreateStats(movementPerTurn: movement, attackRange: range));
            Spawn(simulation, BattleTeam.Player, 1, 1);
            Spawn(simulation, BattleTeam.Player, 1, 0);
            Spawn(simulation, BattleTeam.Enemy, 4, 1);
            Spawn(simulation, BattleTeam.Enemy, 2, 2);
            Spawn(simulation, BattleTeam.Enemy, 0, 3);
            BattleTestUtility.StartTurn(simulation, 1);
            return simulation;
        }

        static BattleSimulation CreateRandomBattle()
        {
            BattleSimulation simulation = new BattleSimulation(4, 3, randomSeed: 37);
            Spawn(simulation, BattleTeam.Player, 0, 1,
                new BattleTestUnit(MercenaryClass.Archer, 1000, 3, 3, 2, 37));
            Spawn(simulation, BattleTeam.Enemy, 1, 1,
                new BattleTestUnit(MercenaryClass.Warrior, 1000, 3, 3, 2, 0), new TraitCollection(new[]
                {
                    new Trait(TraitType.DirectionalPhysicalNegation,
                        new TraitSource(TraitSourceKind.Specialization, 1))
                }), new BattleTraitSettings(0.5f, 0.1f, 0f));
            BattleTestUtility.StartTurn(simulation, 1);
            return simulation;
        }

        static HexCharacter Spawn(BattleSimulation simulation, BattleTeam team, int q, int r,
            BattleTestUnit stats = null, TraitCollection traits = null, BattleTraitSettings traitSettings = null)
        {
            Assert.That(simulation.TrySpawnUnit(team, new HexCoordinate(q, r), stats ?? BattleTestUtility.CreateStats(),
                HexDirection.West, out HexCharacter unit, traits, traitSettings), Is.True);
            return unit;
        }

        static BattleAttackResult Attack(BattleSimulation simulation, int attacker, int target)
        {
            Assert.That(simulation.TryAttack(attacker, target), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            return result;
        }

        bool TryFindTarget(int unitId, out BattleAttackTarget target)
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i].UnitId == unitId)
                {
                    target = _targets[i];
                    return true;
                }
            }

            target = default;
            return false;
        }

        void SeedOldResults()
        {
            _movement.Add(new HexCoordinate(-10, -10));
            _targets.Add(new BattleAttackTarget(-10, true));
        }

        void AssertEmpty()
        {
            Assert.That(_movement, Is.Empty);
            Assert.That(_targets, Is.Empty);
        }
    }
}
