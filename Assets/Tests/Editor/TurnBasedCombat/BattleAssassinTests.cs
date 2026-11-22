using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleAssassinTests
    {
        [Test]
        public void LeapHistorySurvivesMovementCompletionAndIsClearedBeforeTheNextMovement()
        {
            BattleSimulation battle = CreateBattle(8, true, out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            Assert.That(battle.HasStartedAssassinLeap, Is.False);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(battle.IsAssassinLeap, Is.True);
            Assert.That(battle.HasStartedAssassinLeap, Is.True);
            Assert.That(battle.IsWaitingForMovementCompletion, Is.False);
            Assert.That(battle.MovingUnitId, Is.Zero);
            Assert.That(actor.ActionPoints, Is.EqualTo(5));
            battle.CancelAction();
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            battle.StopAction();
            Assert.That(actor.IsInvisible, Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.IsAssassinLeap, Is.False);
            Assert.That(battle.HasStartedAssassinLeap, Is.False);
            Assert.That(battle.TryMove(actor.Id, new HexCoordinate(3, 4)), Is.True);
            Assert.That(battle.MovingUnitId, Is.EqualTo(actor.Id));
            Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new HexCoordinate(3, 4)));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(5));
            Assert.That(actor.MovementRemaining, Is.EqualTo(2));
        }

        [Test]
        public void CommonInputPreviewRejectsChangedLandingInsteadOfReplanning()
        {
            BattleSimulation battle = CreateBattle(3, true, out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            SkillInputPreview preview = battle.PreviewSkill(new SkillInputQuery(SkillType.AssassinLeap, actor.Id, target.Id), path);
            Assert.That(preview.CanUse, Is.True);
            Assert.That(preview.Landing, Is.EqualTo(preview.Request.LeapPreview.Value.Landing));
            SetFacing(target, HexDirection.West);
            Assert.That(battle.CanUseSkill(preview.Request).CanUse, Is.False);
            Assert.That(battle.TryUseSkill(preview.Request), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(3));
            Assert.That(actor.Coordinate, Is.EqualTo(preview.Request.LeapPreview.Value.ActorOrigin));
        }

        [Test]
        public void StartTraitAppliesBeforeTheFirstTurnAndIsNotAutomaticallyAdded()
        {
            BattleSimulation battle = CreateBattle(3, true, out HexCharacter actor, out HexCharacter target);
            Assert.That(actor.IsInvisible, Is.False);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(actor.IsInvisible, Is.True);
            Assert.That(actor.IsStealthed, Is.True);
            Assert.That(actor.MovementRemaining, Is.EqualTo(3));
            Assert.That(battle.IsVisibleTo(BattleTeam.Enemy, actor.Id), Is.False);
            Assert.That(battle.TryStartBattle(), Is.False);
            Assert.That(target.IsInvisible, Is.False);

            BattleSimulation fresh = CreateBattle(3, false, out HexCharacter other, out _);
            BattleTestUtility.StartTurn(fresh, other.Id);
            Assert.That(other.IsInvisible, Is.False);
        }

        [Test]
        public void InvisibilityCanBeUsedInSightAndSurvivesMovementAndTheNextTurn()
        {
            BattleSimulation battle = CreateBattle(8, false, out HexCharacter actor, out HexCharacter target);
            SetFacing(target, HexDirection.West);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.CanEnterStealth(actor.Id), Is.False);
            Assert.That(battle.TryEnterInvisibility(actor.Id), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            Assert.That(actor.MovementRemaining, Is.EqualTo(3));
            Assert.That(battle.TryEnterInvisibility(actor.Id), Is.False);
            Assert.That(battle.TryMove(actor.Id, new HexCoordinate(2, 3)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(actor.IsInvisible, Is.True);
            NextOwnTurn(battle, actor, target);
            Assert.That(actor.MovementRemaining, Is.EqualTo(3));
            Assert.That(actor.IsInvisible, Is.True);
        }

        [Test]
        public void UpgradingStealthDoesNotRefundMovementButRemovesNextTurnPenalty()
        {
            BattleSimulation battle = CreateBattle(8, false, out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryEnterStealth(actor.Id), Is.True);
            Assert.That(actor.MovementRemaining, Is.EqualTo(1));
            NextOwnTurn(battle, actor, target);
            Assert.That(actor.MovementRemaining, Is.EqualTo(2));
            Assert.That(battle.TryEnterInvisibility(actor.Id), Is.True);
            Assert.That(actor.MovementRemaining, Is.EqualTo(2));
            NextOwnTurn(battle, actor, target);
            Assert.That(actor.MovementRemaining, Is.EqualTo(3));
        }

        [TestCase(0, true)]
        [TestCase(1, false)]
        public void OnlyActualDamageRevealsInvisibility(int damage, bool remainsInvisible)
        {
            BattleSimulation battle = CreateBattle(8, true, out HexCharacter actor, out _);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That((int)Invoke(actor, "ApplyDamage", (long)damage), Is.EqualTo(damage));
            Assert.That(actor.IsInvisible, Is.EqualTo(remainsInvisible));
            Assert.That(actor.IsStealthed, Is.EqualTo(remainsInvisible));
        }

        [Test]
        public void LeapSkipsOccupiedMiddleCellsAndPaysOnceEvenWithOnlyThreeAp()
        {
            BattleSimulation battle = CreateBattle(3, true, out HexCharacter actor, out HexCharacter target);
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 4), false, false, true);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out AssassinLeapPreview preview), Is.True);
            Assert.That(preview.Landing, Is.EqualTo(new HexCoordinate(4, 4)));
            Assert.That(battle.TryAssassinLeap(actor.Id, target.Id, preview), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(3));
            Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.From, Is.EqualTo(new HexCoordinate(2, 4)));
            Assert.That(step.To, Is.EqualTo(preview.Landing));
            Assert.That(actor.ActionPoints, Is.Zero);
            Assert.That(actor.MovementRemaining, Is.EqualTo(3));
            Assert.That(battle.HasPendingReactions, Is.False);
            Assert.That(battle.TryCopyRemainingMovementPath(new List<HexCoordinate>()), Is.False);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.False);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(5));
            Assert.That(actor.HasUsedAmbush, Is.False);
            Assert.That(actor.IsStealthed, Is.False);
            Assert.That(battle.TryCompleteAttack(out _), Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.IsAssassinLeap, Is.False);
        }

        [Test]
        public void LeapDoesNotRequireInvisibilityAndIgnoresMovementReactions()
        {
            BattleSimulation battle = CreateBattle(3, false, out HexCharacter actor, out HexCharacter target);
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 4), false, false, true);
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.HasPendingReactions, Is.False);
            Assert.That(actor.Health, Is.EqualTo(20));
        }

        [Test]
        public void LeapAndInvisibilityDoNotRequireMovementPoints()
        {
            BattleSimulation battle = CreateBattle(5, false, out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Invoke(actor, "SpendMovement", 3);
            Assert.That(battle.TryEnterInvisibility(actor.Id), Is.True);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(actor.MovementRemaining, Is.Zero);
            Assert.That(actor.ActionPoints, Is.Zero);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
        }

        [Test]
        public void AnInvisibleReactionAttackerIsRevealedAfterItsAttack()
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 3), 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 4), true, true, true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 4));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryEndTurn(actor.Id), Is.True);
            Assert.That(battle.TryMove(target.Id, new HexCoordinate(4, 4)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.HasPendingReactions, Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(actor.IsInvisible, Is.True);
            Assert.That(battle.TryCompleteAttack(out _), Is.True);
            Assert.That(actor.IsInvisible, Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(3));
        }

        [Test]
        public void OutOfBoundsAndOccupiedLandingsAreRejected()
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 3), 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 4), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(0, 4));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(1, 4));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out _), Is.False);
            SetFacing(target, HexDirection.West);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out _), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(3));
        }

        [TestCase(HexDirection.East)]
        [TestCase(HexDirection.NorthEast)]
        [TestCase(HexDirection.NorthWest)]
        [TestCase(HexDirection.West)]
        [TestCase(HexDirection.SouthWest)]
        [TestCase(HexDirection.SouthEast)]
        public void LandingIsExactlyBehindTheTargetsFacing(HexDirection facing)
        {
            BattleSimulation battle = CreateBattle(3, false, out HexCharacter actor, out HexCharacter target);
            SetFacing(target, facing);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out AssassinLeapPreview preview), Is.True);
            Assert.That(preview.Landing, Is.EqualTo(target.Coordinate.GetNeighbor((HexDirection)(((int)facing + 3) % 6))));
        }

        [TestCase(1, true)]
        [TestCase(4, true)]
        [TestCase(5, false)]
        public void LeapRangeUsesActorToTargetDistance(int distance, bool expected)
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 3), 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 4), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(2 + distance, 4));
            SetFacing(target, HexDirection.West);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out _), Is.EqualTo(expected));
        }

        [TestCase("facing")]
        [TestCase("position")]
        [TestCase("occupied")]
        public void ChangedPreviewIsRejectedBeforeSpending(string change)
        {
            BattleSimulation battle = CreateBattle(3, true, out HexCharacter actor, out HexCharacter target);
            HexCharacter blocker = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 5));
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);

            if (change == "facing")
            {
                SetFacing(target, HexDirection.West);
            }
            else
            {
                int movedId = change == "position" ? target.Id : blocker.Id;
                HexCoordinate destination = change == "position" ? new HexCoordinate(5, 5) : new HexCoordinate(4, 4);
                Assert.That((bool)Invoke(battle.Board, "TryMove", movedId, destination), Is.True);
            }

            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(3));
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(2, 4)));
            Assert.That(actor.IsInvisible, Is.True);
            Assert.That(battle.TrySettleAction(), Is.True);
        }

        [Test]
        public void StartedLeapRejectsUserCancelButSystemStopPreservesLandingAndReveals()
        {
            BattleSimulation battle = CreateBattle(3, true, out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            battle.CancelAction();
            Assert.That(battle.PauseMovement(), Is.False);
            Assert.That(battle.ResumeMovement(), Is.False);
            Assert.That(battle.IsWaitingForMovementCompletion, Is.True);
            battle.StopAction();
            battle.StopAction();
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.False);
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(4, 4)));
            Assert.That(actor.ActionPoints, Is.Zero);
            Assert.That(actor.IsInvisible, Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void UnstartedLeapCanBeCancelledWithoutCostOrRevealing()
        {
            BattleSimulation battle = CreateBattle(3, true, out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            battle.CancelAction();
            Assert.That(actor.ActionPoints, Is.EqualTo(3));
            Assert.That(actor.IsInvisible, Is.True);
            Assert.That(battle.IsBusy, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ParticipantRemovalCompletesTheIssuedStepAndSettlesOnce(bool removeActor)
        {
            BattleSimulation battle = CreateBattle(3, true, out HexCharacter actor, out HexCharacter target);
            HexCharacter ally = Spawn(battle, BattleTeam.Player, new HexCoordinate(1, 1));
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.RemoveUnit(removeActor ? actor.Id : target.Id), Is.True);
            Assert.That(battle.IsWaitingForMovementCompletion, Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);

            if (removeActor)
            {
                Assert.That(battle.CurrentUnitId, Is.EqualTo(ally.Id));
            }
            else
            {
                Assert.That(actor.IsInvisible, Is.False);
                Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(4, 4)));
            }
        }

        [Test]
        public void StoppingAnIssuedAttackCompletesItOnlyOnce()
        {
            BattleSimulation battle = CreateBattle(3, true, out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            battle.StopAction();
            Assert.That(battle.TryCompleteAttack(out _), Is.True);
            Assert.That(target.Health, Is.EqualTo(15));
            Assert.That(battle.TryCompleteAttack(out _), Is.False);
            Assert.That(actor.IsInvisible, Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
        }

        [Test]
        public void InvisibilityStillQualifiesForAmbushAndAttackCompletionRevealsIt()
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 3), 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(4, 4), true, true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(5, 4));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out _), Is.False);
            Assert.That(battle.TryAmbush(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(7));
            Assert.That(actor.IsInvisible, Is.False);
            Assert.That(actor.HasUsedAmbush, Is.True);
        }

        static BattleSimulation CreateBattle(int ap, bool invisible, out HexCharacter actor, out HexCharacter target)
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: ap), 7);
            actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 4), true, invisible);
            target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(5, 4));
            return battle;
        }

        static HexCharacter Spawn(BattleSimulation battle, BattleTeam team, HexCoordinate coordinate,
            bool assassin = false, bool startInvisible = false, bool opportunity = false)
        {
            List<Trait> traits = new List<Trait>();

            if (assassin)
            {
                traits.Add(new Trait(TraitType.RearGuaranteedCritical, new TraitSource(TraitSourceKind.Class, 0)));
            }

            if (startInvisible)
            {
                traits.Add(new Trait(TraitType.BattleStartInvisibility,
                    new TraitSource(TraitSourceKind.Specialization, (int)MercenarySpecialization.Assassin)));
            }

            if (opportunity)
            {
                traits.Add(new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Class, 0)));
            }

            Assert.That(battle.TrySpawnUnit(team, coordinate,
                new BattleTestUnit(assassin ? MercenaryClass.Rogue : MercenaryClass.Warrior,
                    20, 3, 3, 1, 0, initiative: assassin ? 30 : team == BattleTeam.Player ? 20 : 10),
                HexDirection.East, out HexCharacter unit, new TraitCollection(traits.ToArray()),
                specialization: assassin ? MercenarySpecialization.Assassin : MercenarySpecialization.None), Is.True);
            return unit;
        }

        static void QueueLeap(BattleSimulation battle, HexCharacter actor, HexCharacter target)
        {
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out AssassinLeapPreview preview), Is.True);
            Assert.That(battle.TryAssassinLeap(actor.Id, target.Id, preview), Is.True);
        }

        static void NextOwnTurn(BattleSimulation battle, HexCharacter actor, HexCharacter target)
        {
            Assert.That(battle.TryEndTurn(actor.Id), Is.True);
            Assert.That(battle.TryEndTurn(target.Id), Is.True);
        }

        static void SetFacing(HexCharacter unit, HexDirection facing)
        {
            Invoke(unit, "SetFacing", facing, false);
        }

        static object Invoke(object target, string method, params object[] arguments)
        {
            return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, arguments);
        }
    }
}
