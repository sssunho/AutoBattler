using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleMovementReactionTests
    {
        [Test]
        public void CompletedStepIsPreservedWhenTheNextStepIsRejected()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 1, 2, 100);
            Spawn(simulation, BattleTeam.Enemy, 2, 1, 0, opportunity: true);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(3, 2)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new HexCoordinate(2, 2)));
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            int remaining = mover.MovementRemaining;
            HexDirection facing = mover.Facing;
            uint occupancy = simulation.Board.OccupancyVersion;

            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
            Assert.That(mover.Coordinate, Is.EqualTo(new HexCoordinate(2, 2)));
            Assert.That(mover.MovementRemaining, Is.EqualTo(remaining));
            Assert.That(mover.Facing, Is.EqualTo(facing));
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(occupancy));
            Assert.That(simulation.Board.IsOccupied(new HexCoordinate(3, 2)), Is.False);

            CompleteOneReaction(simulation);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(mover.Coordinate, Is.EqualTo(new HexCoordinate(2, 2)));
        }

        [Test]
        public void AdjacentToAdjacentMovementDoesNotReserveReaction()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 1, 2, 100);
            Spawn(simulation, BattleTeam.Enemy, 2, 1, 0, opportunity: true);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(2, 2)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(simulation.HasPendingReactions, Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(mover.Health, Is.EqualTo(mover.MaxHealth));
        }

        [Test]
        public void ReactionsUseFullRoundPriorityIncludingOwnersWhoseTurnsAlreadyEnded()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 10);
            HexCharacter lower = Spawn(simulation, BattleTeam.Enemy, 2, 1, 20, opportunity: true);
            HexCharacter higher = Spawn(simulation, BattleTeam.Enemy, 1, 2, 30, opportunity: true);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Assert.That(simulation.RoundNumber, Is.EqualTo(1));
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep first), Is.True);
            Assert.That(first.AttackerId, Is.EqualTo(higher.Id));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(mover.Id));
            Assert.That(simulation.ActingUnitId, Is.EqualTo(higher.Id));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
            Assert.That(mover.Health, Is.EqualTo(mover.MaxHealth - 1));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep second), Is.True);
            Assert.That(second.AttackerId, Is.EqualTo(lower.Id));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(mover.Health, Is.EqualTo(mover.MaxHealth - 2));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(mover.Id));
        }

        [Test]
        public void EqualPriorityReactionsReuseTheExistingRoundTieOrder()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 19);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 0);
            Spawn(simulation, BattleTeam.Enemy, 1, 2, 10, opportunity: true);
            Spawn(simulation, BattleTeam.Enemy, 2, 1, 10, opportunity: true);
            Assert.That(simulation.TryStartBattle(), Is.True);
            int firstId = simulation.CurrentUnitId.Value;
            Assert.That(simulation.TryEndTurn(firstId), Is.True);
            int secondId = simulation.CurrentUnitId.Value;
            Assert.That(secondId, Is.Not.EqualTo(firstId));
            Assert.That(simulation.TryEndTurn(secondId), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(mover.Id));

            Reserve(simulation, mover, new HexCoordinate(3, 2));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep first), Is.True);
            Assert.That(first.AttackerId, Is.EqualTo(firstId));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.TryBeginAttack(out HexAttackStep second), Is.True);
            Assert.That(second.AttackerId, Is.EqualTo(secondId));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.RoundNumber, Is.EqualTo(1));
        }

        [Test]
        public void ZeroApAndZeroDamageStillConsumeReactionWithoutMovingOrTurningTheMover()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8,
                new BattleRules(startingActionPoints: 0, actionPointsPerTurn: 0), 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 100, defense: 10);
            HexCharacter reactor = Spawn(simulation, BattleTeam.Enemy, 1, 2, 0, opportunity: true);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            int movement = mover.MovementRemaining;
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            Assert.That(mover.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.IsReactionAttack, Is.True);
            Assert.That(reactor.ActionPoints, Is.Zero);
            Assert.That(reactor.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.Zero);
            Assert.That(mover.MovementRemaining, Is.EqualTo(movement));
            Assert.That(mover.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(simulation.TrySettleAction(), Is.True);

            Assert.That(simulation.TryMove(mover.Id, new HexCoordinate(3, 2)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(simulation.HasPendingReactions, Is.False);
        }

        [Test]
        public void EachOwnerTargetPairCanReactAgainOnlyAfterTheNextRound()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter first = Spawn(simulation, BattleTeam.Player, 3, 2, 20);
            HexCharacter second = Spawn(simulation, BattleTeam.Player, 2, 3, 10);
            HexCharacter reactor = Spawn(simulation, BattleTeam.Enemy, 2, 2, 0, opportunity: true);
            BattleTestUtility.StartTurn(simulation, first.Id);
            Reserve(simulation, first, new HexCoordinate(4, 2));
            CompleteOneReaction(simulation);
            Assert.That(simulation.TrySettleAction(), Is.True);
            MoveOneStep(simulation, first, new HexCoordinate(4, 2));
            MoveOneStep(simulation, first, new HexCoordinate(3, 2));
            Assert.That(simulation.TryEndTurn(first.Id), Is.True);

            Reserve(simulation, second, new HexCoordinate(2, 4));
            CompleteOneReaction(simulation);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.TryEndTurn(second.Id), Is.True);
            Assert.That(simulation.TryEndTurn(reactor.Id), Is.True);
            Assert.That(simulation.RoundNumber, Is.EqualTo(2));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(first.Id));
            Reserve(simulation, first, new HexCoordinate(4, 2));
            CompleteOneReaction(simulation);
            Assert.That(first.Health, Is.EqualTo(first.MaxHealth - 2));
            Assert.That(second.Health, Is.EqualTo(second.MaxHealth - 1));
        }

        [Test]
        public void CancelCannotDiscardBatchAndRejectedApproachNeverResumesItsAttack()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 100);
            Spawn(simulation, BattleTeam.Enemy, 1, 2, 10, opportunity: true);
            HexCharacter originalTarget = Spawn(simulation, BattleTeam.Enemy, 5, 2, 0);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            int actionPoints = mover.ActionPoints;
            Assert.That(simulation.TryAttack(mover.Id, originalTarget.Id, new HexCoordinate(4, 2)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            simulation.CancelAction();
            Assert.That(simulation.HasPendingReactions, Is.True);
            Assert.That(simulation.TryEndTurn(mover.Id), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            simulation.CancelAction();
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            simulation.CancelAction();
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(originalTarget.Health, Is.EqualTo(originalTarget.MaxHealth));
            Assert.That(mover.ActionPoints, Is.EqualTo(actionPoints));
        }

        [Test]
        public void RemovingEveryReservedOwnerStillRejectsTheOriginalMovement()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 100);
            HexCharacter reactor = Spawn(simulation, BattleTeam.Enemy, 1, 2, 10, opportunity: true);
            Spawn(simulation, BattleTeam.Enemy, 7, 7, 0);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            Assert.That(simulation.RemoveUnit(reactor.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(mover.Coordinate, Is.EqualTo(new HexCoordinate(2, 2)));
            Assert.That(mover.MovementRemaining, Is.EqualTo(mover.Stats.MovementPerTurn));
        }

        [Test]
        public void LastTurnMoverDeathDiscardsRemainingReactionsAndAdvancesRoundOnlyAtSettlement()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 0, health: 1);
            HexCharacter survivor = Spawn(simulation, BattleTeam.Player, 7, 7, 40);
            HexCharacter first = Spawn(simulation, BattleTeam.Enemy, 1, 2, 30, opportunity: true);
            HexCharacter later = Spawn(simulation, BattleTeam.Enemy, 2, 1, 20, opportunity: true);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep step), Is.True);
            Assert.That(step.AttackerId, Is.EqualTo(first.Id));
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.TargetDied, Is.True);
            Assert.That(simulation.Board.TryGetUnit(mover.Id, out _), Is.False);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.RoundNumber, Is.EqualTo(1));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(mover.Id));
            Assert.That(later.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.RoundNumber, Is.EqualTo(2));
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(survivor.Id));
            Assert.That(simulation.TrySettleAction(), Is.False);
            Assert.That(simulation.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void LastPlayerDeathRequiresSettlementBeforeResultFinalization()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 100, health: 1);
            Spawn(simulation, BattleTeam.Enemy, 1, 2, 0, opportunity: true);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            CompleteOneReaction(simulation);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(simulation.TryFinalizeResult(out _), Is.False);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.TryFinalizeResult(out BattleResult result), Is.True);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(simulation.RoundNumber, Is.EqualTo(1));
        }

        [Test]
        public void StopBeforeIssuanceDiscardsBatchWithoutConsumingUse()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 100);
            HexCharacter reactor = Spawn(simulation, BattleTeam.Enemy, 1, 2, 0, opportunity: true);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            simulation.StopAction();
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(mover.Health, Is.EqualTo(mover.MaxHealth));
            Assert.That(reactor.Facing, Is.EqualTo(HexDirection.West));
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            CompleteOneReaction(simulation);
        }

        [Test]
        public void StopAfterIssuanceCompletesOnceAndDoesNotDispatchTheNextOwner()
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 100);
            HexCharacter first = Spawn(simulation, BattleTeam.Enemy, 1, 2, 20, opportunity: true);
            HexCharacter second = Spawn(simulation, BattleTeam.Enemy, 2, 1, 10, opportunity: true);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            Assert.That(simulation.TryBeginAttack(out HexAttackStep step), Is.True);
            Assert.That(step.AttackerId, Is.EqualTo(first.Id));
            simulation.StopAction();
            simulation.StopAction();
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(mover.Health, Is.EqualTo(mover.MaxHealth - 1));
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(simulation.TrySettleAction(), Is.True);
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            Assert.That(simulation.TryBeginAttack(out step), Is.True);
            Assert.That(step.AttackerId, Is.EqualTo(second.Id));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RemovingIssuedAttackParticipantSettlesWithoutDamageOrDuplicateCompletion(bool removeTarget)
        {
            BattleSimulation simulation = new BattleSimulation(8, 8, randomSeed: 7);
            HexCharacter mover = Spawn(simulation, BattleTeam.Player, 2, 2, 100);
            Spawn(simulation, BattleTeam.Player, 7, 6, 90);
            HexCharacter reactor = Spawn(simulation, BattleTeam.Enemy, 1, 2, 20, opportunity: true);
            Spawn(simulation, BattleTeam.Enemy, 7, 7, 0);
            BattleTestUtility.StartTurn(simulation, mover.Id);
            Reserve(simulation, mover, new HexCoordinate(3, 2));
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.RemoveUnit(removeTarget ? mover.Id : reactor.Id), Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(mover.Health, Is.EqualTo(mover.MaxHealth));
            Assert.That(simulation.TrySettleAction(), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(simulation.TrySettleAction(), Is.False);
        }

        static HexCharacter Spawn(BattleSimulation simulation, BattleTeam team, int q, int r, int initiative,
            bool opportunity = false, int health = 20, int defense = 0)
        {
            TraitCollection traits = opportunity
                ? new TraitCollection(new[]
                {
                    new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Class, 0))
                })
                : TraitCollection.Empty;
            BattleTestUnit stats = new BattleTestUnit(MercenaryClass.Warrior, health, 1, 8, 1, 0,
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
            Assert.That(simulation.HasPendingReactions, Is.True);
            Assert.That(simulation.IsBusy, Is.True);
        }

        static void CompleteOneReaction(BattleSimulation simulation)
        {
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
        }

        static void MoveOneStep(BattleSimulation simulation, HexCharacter mover, HexCoordinate destination)
        {
            Assert.That(simulation.TryMove(mover.Id, destination), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.IsBusy, Is.False);
            Assert.That(mover.Coordinate, Is.EqualTo(destination));
        }
    }
}
