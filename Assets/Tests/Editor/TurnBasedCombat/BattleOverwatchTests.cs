using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleOverwatchTests
    {
        [Test]
        public void EveryDirectionIncludesItsTwoBoundariesAndExcludesOriginAndRear()
        {
            HexCoordinate origin = new HexCoordinate(5, 5);

            for (int direction = 0; direction < 6; direction++)
            {
                for (int offset = 0; offset < 6; offset++)
                {
                    HexCoordinate vector = default(HexCoordinate).GetNeighbor((HexDirection)((direction + offset) % 6));
                    HexCoordinate target = new HexCoordinate(origin.Q + vector.Q * 3, origin.R + vector.R * 3);
                    bool expected = offset == 0 || offset == 1 || offset == 5;
                    Assert.That(BattleOverwatchArea.Contains(origin, (HexDirection)direction, 3, target),
                        Is.EqualTo(expected));
                    Assert.That(BattleOverwatchArea.Contains(origin, (HexDirection)direction, 2, target), Is.False);
                }

                Assert.That(BattleOverwatchArea.Contains(origin, (HexDirection)direction, 3, origin), Is.False);
            }
        }

        [Test]
        public void CopiedCellsUseTheSamePredicateAndStayInsideTheMap()
        {
            List<HexCoordinate> cells = new List<HexCoordinate>();
            HexCoordinate origin = new HexCoordinate(0, 1);
            BattleOverwatchArea.CopyCells(4, 3, origin, HexDirection.East, int.MaxValue, cells);

            for (int r = 0; r < 3; r++)
            {
                for (int q = 0; q < 4; q++)
                {
                    HexCoordinate coordinate = new HexCoordinate(q, r);
                    Assert.That(cells.Contains(coordinate), Is.EqualTo(
                        BattleOverwatchArea.Contains(origin, HexDirection.East, int.MaxValue, coordinate)));
                }
            }

            Assert.That(new HashSet<HexCoordinate>(cells).Count, Is.EqualTo(cells.Count));
            BattleOverwatchArea.CopyCells(4, 3, origin, (HexDirection)6, 3, cells);
            Assert.That(cells, Is.Empty);
            Assert.That(BattleOverwatchArea.Contains(new HexCoordinate(int.MinValue, int.MinValue),
                HexDirection.East, int.MaxValue, new HexCoordinate(int.MaxValue, int.MaxValue)), Is.False);
        }

        [Test]
        public void PreparingSpendsTwoPointsTurnsAndExpiresOnlyOnTheOwnersNextTurn()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter archer, out HexCharacter enemy);
            HexCharacter secondEnemy = Spawn(simulation, BattleTeam.Enemy, 11, 11, 10);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.CanPrepareOverwatch(archer.Id), Is.True);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.NorthEast), Is.True);
            Assert.That(archer.ActionPoints, Is.EqualTo(2));
            Assert.That(archer.Facing, Is.EqualTo(HexDirection.NorthEast));
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(enemy.Id));
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.West), Is.False);
            Assert.That(simulation.TryEndTurn(enemy.Id), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(secondEnemy.Id));
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(simulation.TryEndTurn(secondEnemy.Id), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(archer.Id));
            Assert.That(archer.IsOverwatching, Is.False);
            Assert.That(archer.ActionPoints, Is.EqualTo(6));
        }

        [Test]
        public void InvalidPreparationCannotChangePointsFacingOrTurn()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter archer, out HexCharacter enemy);
            Assert.That(simulation.CanPrepareOverwatch(archer.Id), Is.False);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, (HexDirection)6), Is.False);
            Assert.That(archer.ActionPoints, Is.EqualTo(4));
            Assert.That(archer.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(archer.IsOverwatching, Is.False);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(archer.Id));
            Assert.That(simulation.TryMove(archer.Id, new HexCoordinate(4, 5)), Is.True);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.False);
            simulation.CancelAction();
            Assert.That(simulation.TryEndTurn(archer.Id), Is.True);
            Assert.That(simulation.TryPrepareOverwatch(enemy.Id, HexDirection.West), Is.False);

            BattleSimulation poor = CreateBattle(out HexCharacter poorArcher, out _, startingPoints: 1);
            BattleTestUtility.StartTurn(poor, poorArcher.Id);
            Assert.That(poor.TryPrepareOverwatch(poorArcher.Id, HexDirection.East), Is.False);
            Assert.That(poorArcher.ActionPoints, Is.EqualTo(1));
        }

        [Test]
        public void MovementInsideTheAreaReservesAndConsumesOnlyWhenAttackIsIssued()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter archer, out HexCharacter enemy,
                startingPoints: 2);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Reserve(simulation, enemy, new HexCoordinate(7, 4));
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(archer.ActionPoints, Is.Zero);
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(6, 4)));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(8));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep attack), Is.True);
            Assert.That(attack.AttackerId, Is.EqualTo(archer.Id));
            Assert.That(archer.IsOverwatching, Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(18));
            Assert.That(archer.ActionPoints, Is.Zero);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.TryMove(enemy.Id, new HexCoordinate(7, 4)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
        }

        [Test]
        public void EnteringTheAreaDoesNotReactUntilTheNextMovementDeclaration()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter archer, out HexCharacter enemy,
                enemyQ: 8);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Assert.That(simulation.TryMove(enemy.Id, new HexCoordinate(6, 4)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new HexCoordinate(7, 4)));
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(7, 4)));
            Assert.That(enemy.MovementRemaining, Is.EqualTo(7));
        }

        [Test]
        public void OpportunityPrecedesOverwatchAndItsFacingChangePreservesThePreparedDirection()
        {
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Selection, 1))
            });
            BattleSimulation simulation = new BattleSimulation(12, 12, randomSeed: 3);
            HexCharacter archer = Spawn(simulation, BattleTeam.Player, 4, 4, 30, traits: traits);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 5, 4, 20);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.NorthEast), Is.True);
            Reserve(simulation, enemy, new HexCoordinate(6, 4));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep first), Is.True);
            Assert.That(first.AttackerId, Is.EqualTo(archer.Id));
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(archer.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(archer.OverwatchDirection, Is.EqualTo(HexDirection.NorthEast));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.TryBeginAttack(out HexAttackStep second), Is.True);
            Assert.That(second.AttackerId, Is.EqualTo(archer.Id));
            Assert.That(archer.IsOverwatching, Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(16));
            Assert.That(simulation.TrySettleAction(), Is.True);
        }

        [Test]
        public void EarlierLethalReactionKeepsUnissuedOverwatchPrepared()
        {
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Class, 0))
            });
            BattleSimulation simulation = new BattleSimulation(12, 12, randomSeed: 3);
            HexCharacter killer = Spawn(simulation, BattleTeam.Player, 5, 3, 40,
                unitClass: MercenaryClass.Warrior, traits: traits);
            HexCharacter archer = Spawn(simulation, BattleTeam.Player, 4, 4, 30);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 5, 4, 20, health: 1);
            Spawn(simulation, BattleTeam.Enemy, 11, 11, 10);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Reserve(simulation, enemy, new HexCoordinate(6, 4));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep attack), Is.True);
            Assert.That(attack.AttackerId, Is.EqualTo(killer.Id));
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.TargetDied, Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(archer.IsOverwatching, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StoppingConsumesOnlyAnAlreadyIssuedOverwatch(bool issue)
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter archer, out HexCharacter enemy);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Reserve(simulation, enemy, new HexCoordinate(7, 4));

            if (issue)
            {
                Assert.That(simulation.TryBeginAttack(out _), Is.True);
            }

            simulation.StopAction();

            if (issue)
            {
                Assert.That(simulation.TryCompleteAttack(out _), Is.True);
                Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            }

            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(archer.IsOverwatching, Is.EqualTo(!issue));
            Assert.That(enemy.Health, Is.EqualTo(issue ? 18 : 20));
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
        }

        [Test]
        public void RemovedTargetBeforeIssuanceDoesNotConsumePreparation()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter archer, out HexCharacter enemy);
            Spawn(simulation, BattleTeam.Enemy, 11, 11, 10);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Reserve(simulation, enemy, new HexCoordinate(7, 4));
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(archer.IsOverwatching, Is.True);
        }

        [Test]
        public void ZeroDamageStillConsumesOverwatch()
        {
            BattleSimulation simulation = new BattleSimulation(12, 12, randomSeed: 3);
            HexCharacter archer = Spawn(simulation, BattleTeam.Player, 4, 4, 30);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 6, 4, 20, defense: 10);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Reserve(simulation, enemy, new HexCoordinate(7, 4));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.Zero);
            Assert.That(archer.IsOverwatching, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExternalMovementOrFacingChangeClearsPreparation(bool move)
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter archer, out HexCharacter enemy);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Reserve(simulation, enemy, new HexCoordinate(7, 4));

            if (move)
            {
                object result = typeof(BattleBoard).GetMethod("TryMove", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(simulation.Board, new object[] { archer.Id, new HexCoordinate(4, 5) });
                Assert.That(result, Is.EqualTo(true));
            }
            else
            {
                typeof(HexCharacter).GetMethod("SetFacing", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(archer, new object[] { HexDirection.West, false });
            }

            Assert.That(archer.IsOverwatching, Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(enemy.Coordinate, Is.EqualTo(new HexCoordinate(6, 4)));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
        }

        [Test]
        public void AllActiveAreasCanBeCopiedDuringEnemyTurnAndReaction()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter first, out HexCharacter enemy);
            HexCharacter second = Spawn(simulation, BattleTeam.Player, 4, 6, 25);
            BattleTestUtility.StartTurn(simulation, first.Id);
            Assert.That(simulation.TryPrepareOverwatch(first.Id, HexDirection.East), Is.True);
            Assert.That(simulation.TryPrepareOverwatch(second.Id, HexDirection.East), Is.True);
            List<BattleOverwatchState> states = new List<BattleOverwatchState>();
            simulation.CopyOverwatchStates(states);
            Assert.That(states.Count, Is.EqualTo(2));
            Reserve(simulation, enemy, new HexCoordinate(7, 4));
            simulation.CopyOverwatchStates(states);
            Assert.That(states.Count, Is.EqualTo(2));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            simulation.CopyOverwatchStates(states);
            Assert.That(states.Count, Is.EqualTo(1));
            Assert.That(states[0].UnitId, Is.EqualTo(second.Id));
            Assert.That(states[0].Coordinate, Is.EqualTo(second.Coordinate));
            Assert.That(states[0].Direction, Is.EqualTo(HexDirection.East));
            Assert.That(states[0].Range, Is.EqualTo(second.Skills.BasicAttack.MaxRange));
        }

        [Test]
        public void RemovingTheLastOpponentClearsPreparationAndCopiedAreas()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter archer, out HexCharacter enemy);
            BattleTestUtility.StartTurn(simulation, archer.Id);
            Assert.That(simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);
            Assert.That(archer.IsOverwatching, Is.False);
            List<BattleOverwatchState> states = new List<BattleOverwatchState>();
            simulation.CopyOverwatchStates(states);
            Assert.That(states, Is.Empty);
        }

        static BattleSimulation CreateBattle(out HexCharacter archer, out HexCharacter enemy,
            int enemyQ = 6, int startingPoints = 4)
        {
            BattleSimulation simulation = new BattleSimulation(12, 12,
                new BattleRules(startingActionPoints: startingPoints), 3);
            archer = Spawn(simulation, BattleTeam.Player, 4, 4, 30);
            enemy = Spawn(simulation, BattleTeam.Enemy, enemyQ, 4, 20);
            return simulation;
        }

        static HexCharacter Spawn(BattleSimulation simulation, BattleTeam team, int q, int r, int initiative,
            MercenaryClass unitClass = MercenaryClass.Archer, int health = 20, int defense = 0,
            TraitCollection traits = null)
        {
            BattleTestUnit stats = new BattleTestUnit(unitClass, health, 2, 8, 3, 0,
                physicalDefense: defense, initiative: initiative);
            Assert.That(simulation.TrySpawnUnit(team, new HexCoordinate(q, r), stats, HexDirection.West,
                out HexCharacter unit, traits), Is.True);
            return unit;
        }

        static void Reserve(BattleSimulation simulation, HexCharacter mover, HexCoordinate destination)
        {
            Assert.That(simulation.TryMove(mover.Id, destination), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
        }
    }
}
