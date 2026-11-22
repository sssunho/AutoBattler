using RaggedBanner.HexGrid;
using NUnit.Framework;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleAttackTests
    {
        [Test]
        public void AdjacentAttackStaysBusyUntilPresentationCompletesWithoutEndingTurn()
        {
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);

            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryEndTurn(attacker.Id), Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(simulation.TryBeginAttack(out HexAttackStep attack), Is.True);
            Assert.That(attack.AttackerId, Is.EqualTo(attacker.Id));
            Assert.That(attack.TargetId, Is.EqualTo(target.Id));
            Assert.That(attack.TargetCoordinate, Is.EqualTo(target.Coordinate));
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.False);
            Assert.That(simulation.TryMove(attacker.Id, new HexCoordinate(2, 0)), Is.False);
            Assert.That(simulation.TryEndTurn(attacker.Id), Is.False);
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(attacker.Id));
            Assert.That(attacker.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);
        }

        [Test]
        public void ApproachWaitsForEveryMovementPresentationIncludingLastHex()
        {
            BattleSimulation simulation = new BattleSimulation(5, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);

            for (int q = 1; q <= 3; q++)
            {
                Assert.That(simulation.TryBeginMovementStep(out HexMovementStep move), Is.True);
                Assert.That(move.To, Is.EqualTo(new HexCoordinate(q, 0)));
                Assert.That(simulation.IsBusy, Is.True);
                Assert.That(simulation.TryBeginAttack(out _), Is.False);
                Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.False);
                Assert.That(simulation.TryEndTurn(attacker.Id), Is.False);
                Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(attacker.Coordinate.DistanceTo(target.Coordinate), Is.EqualTo(1));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
        }

        [Test]
        public void SpecifiedAttackPositionIsReachedBeforeAttackingChosenTarget()
        {
            BattleSimulation simulation = new BattleSimulation(5, 3, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(movementPerTurn: 5), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter nearbyTarget);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter target);
            HexCoordinate attackPosition = new HexCoordinate(4, 1);
            BattleTestUtility.StartTurn(simulation, attacker.Id);

            Assert.That(simulation.TryAttack(attacker.Id, target.Id, attackPosition), Is.True);
            Assert.That(attacker.Coordinate, Is.EqualTo(new HexCoordinate(0, 1)));
            Assert.That(attacker.MovementRemaining, Is.EqualTo(5));
            Assert.That(attacker.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.TryAttack(attacker.Id, nearbyTarget.Id, attacker.Coordinate), Is.False);
            int steps = 0;
            bool enteredAttackRangeBeforeDestination = false;

            while (simulation.TryBeginMovementStep(out HexMovementStep move))
            {
                steps++;
                Assert.That(steps, Is.LessThanOrEqualTo(5));
                Assert.That(move.To, Is.EqualTo(attacker.Coordinate));
                Assert.That(attacker.MovementRemaining, Is.EqualTo(5 - steps));
                Assert.That(attacker.ActionPoints, Is.EqualTo(4));
                Assert.That(target.Health, Is.EqualTo(10));
                Assert.That(simulation.TargetUnitId, Is.EqualTo(target.Id));
                Assert.That(simulation.TryBeginAttack(out _), Is.False);
                Assert.That(simulation.TryCompleteMovementStep(), Is.True);

                if (attacker.Coordinate != attackPosition)
                {
                    enteredAttackRangeBeforeDestination |= attacker.Coordinate.DistanceTo(target.Coordinate) <= attacker.Skills.BasicAttack.MaxRange;
                    Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Moving));
                    Assert.That(simulation.TryBeginAttack(out _), Is.False);
                }
            }

            Assert.That(steps, Is.EqualTo(5));
            Assert.That(enteredAttackRangeBeforeDestination, Is.True);
            Assert.That(attacker.Coordinate, Is.EqualTo(attackPosition));
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep attack), Is.True);
            Assert.That(attack.TargetId, Is.EqualTo(target.Id));
            Assert.That(attacker.ActionPoints, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(10));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(target.Health, Is.EqualTo(7));
            Assert.That(nearbyTarget.Health, Is.EqualTo(10));
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(attacker.MovementRemaining, Is.Zero);
            Assert.That(attacker.Coordinate, Is.EqualTo(attackPosition));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(attacker.Id));
        }

        [Test]
        public void SpecifiedCurrentPositionUsesActualRangeWithoutMovement()
        {
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 0, attackRange: 2), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, attacker.Id);

            Assert.That(simulation.TryAttack(attacker.Id, target.Id, attacker.Coordinate), Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(attacker.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(attacker.MovementRemaining, Is.Zero);
            Assert.That(target.Health, Is.EqualTo(7));
        }

        [TestCase(-1, 0)]
        [TestCase(int.MinValue, 0)]
        [TestCase(5, 0)]
        [TestCase(0, 2)]
        [TestCase(3, 0)]
        [TestCase(2, 1)]
        [TestCase(0, 0)]
        public void InvalidSpecifiedPositionDoesNotReserveActionOrSpendResources(int q, int r)
        {
            BattleSimulation simulation = new BattleSimulation(5, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter target);
            simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _);
            BattleTestUtility.StartTurn(simulation, attacker.Id);

            Assert.That(simulation.TryAttack(attacker.Id, target.Id, new HexCoordinate(q, r)), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.TargetUnitId, Is.Zero);
            Assert.That(attacker.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(attacker.MovementRemaining, Is.EqualTo(3));
            Assert.That(attacker.ActionPoints, Is.EqualTo(4));
            Assert.That(target.Health, Is.EqualTo(10));
            Assert.That(simulation.TryAttack(attacker.Id, target.Id, new HexCoordinate(2, 0)), Is.True);
        }

        [TestCase(0, 4)]
        [TestCase(1, 4)]
        [TestCase(3, 1)]
        public void SpecifiedPositionRequiresEnoughMovementAndActionPoints(int movement, int actionPoints)
        {
            BattleRules rules = new BattleRules(startingActionPoints: actionPoints, actionPointsPerTurn: 0);
            BattleSimulation simulation = new BattleSimulation(4, 1, rules, 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: movement), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, attacker.Id);

            Assert.That(simulation.TryAttack(attacker.Id, target.Id, new HexCoordinate(2, 0)), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.TargetUnitId, Is.Zero);
            Assert.That(attacker.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(attacker.MovementRemaining, Is.EqualTo(movement));
            Assert.That(attacker.ActionPoints, Is.EqualTo(actionPoints));
            Assert.That(target.Health, Is.EqualTo(10));
        }

        [Test]
        public void BlockedSpecifiedPositionDoesNotUseAnotherAttackPosition()
        {
            BattleSimulation simulation = new BattleSimulation(5, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 4, attackRange: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter target);
            simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out _);
            BattleTestUtility.StartTurn(simulation, attacker.Id);

            Assert.That(simulation.TryAttack(attacker.Id, target.Id, new HexCoordinate(3, 0)), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(attacker.MovementRemaining, Is.EqualTo(4));
            Assert.That(attacker.ActionPoints, Is.EqualTo(4));
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep move), Is.True);
            Assert.That(move.To, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
        }

        [Test]
        public void OccupiedNearestAttackPositionUsesShortestReachableAlternative()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);
            simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter blocker);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);
            int steps = 0;

            while (simulation.TryBeginMovementStep(out HexMovementStep move))
            {
                Assert.That(move.To, Is.Not.EqualTo(target.Coordinate));
                Assert.That(move.To, Is.Not.EqualTo(blocker.Coordinate));
                Assert.That(move.From.DistanceTo(move.To), Is.EqualTo(1));
                Assert.That(simulation.TryCompleteMovementStep(), Is.True);
                steps++;
                Assert.That(steps, Is.LessThanOrEqualTo(3));
            }

            Assert.That(steps, Is.EqualTo(3));
            Assert.That(attacker.Coordinate, Is.EqualTo(new HexCoordinate(2, 1)));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
        }

        [TestCase(2)]
        [TestCase(1)]
        public void BlockedAttackPositionOrCorridorDoesNothing(int blockerQ)
        {
            BattleSimulation simulation = new BattleSimulation(4, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);
            simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(blockerQ, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out _);

            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.TargetUnitId, Is.Zero);
            Assert.That(attacker.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(simulation.Board.IsOccupied(attacker.Coordinate), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(attacker.Id));
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
        }

        [Test]
        public void InvalidTargetsAndNonCurrentAttackerAreRejected()
        {
            BattleSimulation simulation = new BattleSimulation(3, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter ally);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter enemy);

            Assert.That(simulation.TrySpawnUnit((BattleTeam)999, new HexCoordinate(2, 1),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out _), Is.False);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Assert.That(simulation.TryAttack(attacker.Id, attacker.Id), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, ally.Id), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, 999), Is.False);
            Assert.That(simulation.TryAttack(999, enemy.Id), Is.False);
            Assert.That(simulation.TryAttack(enemy.Id, attacker.Id), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, attacker.Id, attacker.Coordinate), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, ally.Id, attacker.Coordinate), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, 999, attacker.Coordinate), Is.False);
            Assert.That(simulation.TryAttack(999, enemy.Id, attacker.Coordinate), Is.False);
            Assert.That(simulation.TryAttack(enemy.Id, attacker.Id, enemy.Coordinate), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void RemovingTargetDuringApproachPreservesIssuedStepAndCancelsRemainingAction(bool lastEnemy, bool specifiedPosition)
        {
            BattleSimulation simulation = new BattleSimulation(5, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);

            if (!lastEnemy)
            {
                simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 1),
                    BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out _);
            }

            BattleTestUtility.StartTurn(simulation, attacker.Id);
            bool accepted = specifiedPosition
                ? simulation.TryAttack(attacker.Id, target.Id, new HexCoordinate(3, 0))
                : simulation.TryAttack(attacker.Id, target.Id);
            Assert.That(accepted, Is.True);
            simulation.TryBeginMovementStep(out HexMovementStep move);

            Assert.That(simulation.RemoveUnit(target.Id), Is.True);
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(simulation.TargetUnitId, Is.Zero);
            Assert.That(attacker.Coordinate, Is.EqualTo(move.To));
            Assert.That(simulation.Board.IsOccupied(move.To), Is.True);
            Assert.That(simulation.Board.IsOccupied(target.Coordinate), Is.False);
            Assert.That(simulation.State, Is.EqualTo(lastEnemy ? BattleState.Waiting : BattleState.Running));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(lastEnemy ? (int?)null : attacker.Id));
            Assert.That(attacker.ActionPoints, Is.EqualTo(4));
            Assert.That(attacker.MovementRemaining, Is.EqualTo(2));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
        }

        [TestCase(1, false)]
        [TestCase(3, false)]
        [TestCase(1, true)]
        [TestCase(3, true)]
        public void CancellingApproachCompletesIssuedStepWithoutStartingAttack(int issuedSteps, bool specifiedPosition)
        {
            BattleSimulation simulation = new BattleSimulation(5, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            bool accepted = specifiedPosition
                ? simulation.TryAttack(attacker.Id, target.Id, new HexCoordinate(3, 0))
                : simulation.TryAttack(attacker.Id, target.Id);
            Assert.That(accepted, Is.True);

            for (int i = 0; i < issuedSteps; i++)
            {
                Assert.That(simulation.TryBeginMovementStep(out _), Is.True);

                if (i < issuedSteps - 1)
                {
                    Assert.That(simulation.TryCompleteMovementStep(), Is.True);
                }
            }

            simulation.CancelAction();
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(simulation.TargetUnitId, Is.Zero);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(attacker.Coordinate, Is.EqualTo(new HexCoordinate(issuedSteps, 0)));
            Assert.That(attacker.MovementRemaining, Is.EqualTo(3 - issuedSteps));
            Assert.That(attacker.ActionPoints, Is.EqualTo(4));
            Assert.That(target.Health, Is.EqualTo(10));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
        }

        [Test]
        public void RemovingAttackerDuringAttackKeepsIssuedActionUntilCompletionAndSettlement()
        {
            BattleSimulation simulation = new BattleSimulation(2, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            simulation.TryAttack(attacker.Id, target.Id);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);

            Assert.That(simulation.RemoveUnit(attacker.Id), Is.True);
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(simulation.ActingUnitId, Is.EqualTo(attacker.Id));
            Assert.That(simulation.TargetUnitId, Is.EqualTo(target.Id));
            Assert.That(simulation.CurrentUnitId, Is.Null);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(attacker.ActionPoints, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(10));
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(simulation.TryFinalizeResult(out _), Is.False);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.ActingUnitId, Is.Zero);
            Assert.That(simulation.TargetUnitId, Is.Zero);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(target.Health, Is.EqualTo(10));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RemovingTargetDuringAttackPreservesIssuedStepUntilCompletion(bool lastEnemy)
        {
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);

            if (!lastEnemy)
            {
                simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 0),
                    BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out _);
            }

            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.RemoveUnit(target.Id), Is.True);
            simulation.CancelAction();

            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(simulation.ActingUnitId, Is.EqualTo(attacker.Id));
            Assert.That(simulation.TargetUnitId, Is.EqualTo(target.Id));
            Assert.That(simulation.State, Is.EqualTo(lastEnemy ? BattleState.Waiting : BattleState.Running));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(lastEnemy ? (int?)null : attacker.Id));
            Assert.That(simulation.TryEndTurn(attacker.Id), Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.ActingUnitId, Is.Zero);
            Assert.That(simulation.TargetUnitId, Is.Zero);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(attacker.ActionPoints, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(10));
        }

        [Test]
        public void PauseAndResumeApproachPreserveChosenTarget()
        {
            BattleSimulation simulation = new BattleSimulation(4, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out _);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            simulation.TryAttack(attacker.Id, target.Id);
            simulation.TryBeginMovementStep(out _);
            Assert.That(simulation.PauseMovement(), Is.True);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TargetUnitId, Is.EqualTo(target.Id));
            Assert.That(simulation.ResumeMovement(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryBeginAttack(out HexAttackStep attack), Is.True);
            Assert.That(attack.TargetId, Is.EqualTo(target.Id));
        }

        [Test]
        public void CancellationCannotReplaceIssuedAttackAndCompletionAppliesDamageOnce()
        {
            BattleSimulation simulation = new BattleSimulation(3, 2, randomSeed: 7);
            simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter attacker);
            simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter target);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            simulation.CancelAction();
            simulation.CancelAction();
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(simulation.ActingUnitId, Is.EqualTo(attacker.Id));
            Assert.That(simulation.TargetUnitId, Is.EqualTo(target.Id));
            Assert.That(attacker.ActionPoints, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(10));
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TryMove(attacker.Id, new HexCoordinate(0, 1)), Is.False);
            Assert.That(simulation.TryEndTurn(attacker.Id), Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(attacker.ActionPoints, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(7));
            Assert.That(simulation.TryAttack(attacker.Id, target.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(attacker.ActionPoints, Is.Zero);
            Assert.That(target.Health, Is.EqualTo(4));
        }
    }
}
