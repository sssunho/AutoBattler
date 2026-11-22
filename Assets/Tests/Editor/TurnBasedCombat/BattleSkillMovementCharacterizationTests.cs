using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSkillMovementCharacterizationTests
    {
        static readonly HexCoordinate _assassinOrigin = new HexCoordinate(2, 4);
        static readonly HexCoordinate _leapTargetOrigin = new HexCoordinate(5, 4);
        static readonly HexCoordinate _landing = new HexCoordinate(4, 4);
        static readonly HexCoordinate _pioneerOrigin = new HexCoordinate(2, 5);
        static readonly HexCoordinate _pullTargetOrigin = new HexCoordinate(6, 5);

        [TestCase(false)]
        [TestCase(true)]
        public void LeapAttackKillingTheTargetSettlesOnceAfterTheAttack(bool lastEnemy)
        {
            BattleSimulation battle = NewBattle(3);
            HexCharacter actor = SpawnAssassin(battle, _assassinOrigin);
            HexCharacter target = SpawnEnemy(battle, _leapTargetOrigin, health: 1);

            if (!lastEnemy)
            {
                SpawnEnemy(battle, new HexCoordinate(10, 10));
            }

            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);

            Assert.That(result.TargetDied, Is.True);
            Assert.That(battle.Board.TryGetUnit(target.Id, out _), Is.False);
            Assert.That(battle.State, Is.EqualTo(lastEnemy ? BattleState.Waiting : BattleState.Running));
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.IsAssassinLeap, Is.True);
            Assert.That(battle.HasStartedAssassinLeap, Is.True);
            Assert.That(actor.IsInvisible, Is.False);
            Assert.That(actor.Coordinate, Is.EqualTo(_landing));
            Assert.That(actor.ActionPoints, Is.Zero);
            Assert.That(battle.TryCompleteAttack(out _), Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(battle.IsAssassinLeap, Is.False);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(lastEnemy ? (int?)null : actor.Id));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RemovingALeapParticipantDuringTheAttackDefersSettlingUntilTheAttackEnds(bool removeActor)
        {
            BattleSimulation battle = NewBattle(3);
            HexCharacter actor = SpawnAssassin(battle, _assassinOrigin);
            HexCharacter target = SpawnEnemy(battle, _leapTargetOrigin);
            HexCharacter ally = SpawnAlly(battle, new HexCoordinate(1, 1));
            SpawnEnemy(battle, new HexCoordinate(10, 10));
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);

            Assert.That(battle.RemoveUnit(removeActor ? actor.Id : target.Id), Is.True);

            Assert.That(battle.State, Is.EqualTo(BattleState.Running));
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(battle.IsAttackDamageResolved, Is.False);
            Assert.That(battle.TrySettleAction(), Is.False);
            Assert.That(battle.TryCompleteAttack(out _), Is.False);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.IsAssassinLeap, Is.True);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);
            Assert.That(battle.IsBusy, Is.False);

            if (removeActor)
            {
                Assert.That(battle.CurrentUnitId, Is.EqualTo(ally.Id));
                Assert.That(target.Health, Is.EqualTo(20));
            }
            else
            {
                Assert.That(battle.CurrentUnitId, Is.EqualTo(actor.Id));
                Assert.That(actor.IsInvisible, Is.False);
                Assert.That(actor.Coordinate, Is.EqualTo(_landing));
                Assert.That(actor.ActionPoints, Is.Zero);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RemovingALeapParticipantBeforeTheAttackSettlesImmediately(bool removeActor)
        {
            BattleSimulation battle = NewBattle(3);
            HexCharacter actor = SpawnAssassin(battle, _assassinOrigin);
            HexCharacter target = SpawnEnemy(battle, _leapTargetOrigin);
            HexCharacter ally = SpawnAlly(battle, new HexCoordinate(1, 1));
            SpawnEnemy(battle, new HexCoordinate(10, 10));
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));

            Assert.That(battle.RemoveUnit(removeActor ? actor.Id : target.Id), Is.True);

            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.TryBeginAttack(out _), Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);

            if (removeActor)
            {
                Assert.That(battle.CurrentUnitId, Is.EqualTo(ally.Id));
            }
            else
            {
                Assert.That(battle.CurrentUnitId, Is.EqualTo(actor.Id));
                Assert.That(actor.IsInvisible, Is.False);
                Assert.That(actor.Coordinate, Is.EqualTo(_landing));
                Assert.That(actor.ActionPoints, Is.Zero);
            }
        }

        [Test]
        public void RemovingTheAnchorDuringPullSelfFinishesAfterTheIssuedStep()
        {
            BattleSimulation battle = NewBattle(8);
            HexCharacter actor = SpawnPioneer(battle, _pioneerOrigin);
            HexCharacter target = SpawnEnemy(battle, _pullTargetOrigin);
            SpawnEnemy(battle, new HexCoordinate(10, 10));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryEnterStealth(actor.Id), Is.True);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);

            Assert.That(battle.RemoveUnit(target.Id), Is.True);

            Assert.That(battle.IsWaitingForMovementCompletion, Is.True);
            Assert.That(actor.IsStealthed, Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(3, 5)));
            Assert.That(actor.IsStealthed, Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(actor.Id));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PullSelfActorKilledByTheGroundStopsTheSkill(bool lastPlayer)
        {
            BattleSimulation battle = NewBattle(8);
            HexCharacter actor = SpawnPioneer(battle, _pioneerOrigin, health: 1);
            HexCharacter target = SpawnEnemy(battle, _pullTargetOrigin);
            HexCharacter ally = lastPlayer ? null : SpawnAlly(battle, new HexCoordinate(0, 0));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Burn(battle, new HexCoordinate(3, 5));
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);

            Assert.That(battle.TryCompleteMovementStep(), Is.True);

            Assert.That(battle.Board.TryGetUnit(actor.Id, out _), Is.False);
            Assert.That(battle.State, Is.EqualTo(lastPlayer ? BattleState.Waiting : BattleState.Running));
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.IsWaitingForMovementCompletion, Is.False);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.TryCompleteMovementStep(), Is.False);
            Assert.That(target.Coordinate, Is.EqualTo(_pullTargetOrigin));
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);
            Assert.That(battle.IsBusy, Is.False);

            if (!lastPlayer)
            {
                Assert.That(battle.CurrentUnitId, Is.EqualTo(ally.Id));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PulledTargetKilledByTheGroundStopsTheSkill(bool lastEnemy)
        {
            BattleSimulation battle = NewBattle(8);
            HexCharacter actor = SpawnPioneer(battle, _pioneerOrigin);
            HexCharacter target = SpawnEnemy(battle, _pullTargetOrigin, health: 1);

            if (!lastEnemy)
            {
                SpawnEnemy(battle, new HexCoordinate(10, 10));
            }

            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryEnterStealth(actor.Id), Is.True);
            Burn(battle, new HexCoordinate(5, 5));
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);

            Assert.That(battle.TryCompleteMovementStep(), Is.True);

            Assert.That(battle.Board.TryGetUnit(target.Id, out _), Is.False);
            Assert.That(battle.State, Is.EqualTo(lastEnemy ? BattleState.Waiting : BattleState.Running));
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.IsStealthed, Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(4));
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(lastEnemy ? (int?)null : actor.Id));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AssassinKilledOnTheLandingSkipsTheAttack(bool lastPlayer)
        {
            BattleSimulation battle = NewBattle(3);
            HexCharacter actor = SpawnAssassin(battle, _assassinOrigin, health: 1);
            HexCharacter target = SpawnEnemy(battle, _leapTargetOrigin);
            HexCharacter ally = lastPlayer ? null : SpawnAlly(battle, new HexCoordinate(1, 1));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Burn(battle, _landing);
            QueueLeap(battle, actor, target);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);

            Assert.That(battle.TryCompleteMovementStep(), Is.True);

            Assert.That(battle.Board.TryGetUnit(actor.Id, out _), Is.False);
            Assert.That(battle.State, Is.EqualTo(lastPlayer ? BattleState.Waiting : BattleState.Running));
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.TryBeginAttack(out _), Is.False);
            Assert.That(target.Health, Is.EqualTo(20));
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);
            Assert.That(battle.IsBusy, Is.False);

            if (!lastPlayer)
            {
                Assert.That(battle.CurrentUnitId, Is.EqualTo(ally.Id));
            }
        }

        [Test]
        public void SecondPioneerUseCanBeCancelledAndIsRevalidatedAndChargedAgain()
        {
            BattleSimulation battle = NewBattle(8);
            HexCharacter actor = SpawnPioneer(battle, _pioneerOrigin);
            HexCharacter target = SpawnEnemy(battle, _pullTargetOrigin);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            FinishPioneerSkill(battle);
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            HexCoordinate afterFirst = actor.Coordinate;
            Assert.That(afterFirst, Is.EqualTo(new HexCoordinate(5, 5)));
            MoveExternally(battle, target.Id, new HexCoordinate(9, 5));

            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            Assert.That(battle.HasStartedPioneerMovement, Is.False);
            battle.CancelAction();
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(6));

            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            MoveExternally(battle, target.Id, new HexCoordinate(9, 6));
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            Assert.That(actor.Coordinate, Is.EqualTo(afterFirst));
            Assert.That(battle.TrySettleAction(), Is.True);

            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            FinishPioneerSkill(battle);
            Assert.That(actor.ActionPoints, Is.EqualTo(4));
            Assert.That(actor.Coordinate.DistanceTo(target.Coordinate), Is.EqualTo(1));
        }

        [Test]
        public void LeapAfterAnotherUnitsPioneerSkillStartsFromAFreshExecution()
        {
            BattleSimulation battle = NewBattle(8);
            HexCharacter pioneer = SpawnPioneer(battle, new HexCoordinate(2, 8));
            HexCharacter assassin = SpawnAssassin(battle, _assassinOrigin, initiative: 28);
            HexCharacter pulled = SpawnEnemy(battle, new HexCoordinate(6, 8));
            HexCharacter leapTarget = SpawnEnemy(battle, _leapTargetOrigin, initiative: 9);
            BattleTestUtility.StartTurn(battle, pioneer.Id);
            Assert.That(battle.TryPioneerSkill(pioneer.Id, pulled.Id, SkillType.PullTarget), Is.True);
            FinishPioneerSkill(battle);
            Assert.That(battle.TryEndTurn(pioneer.Id), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(assassin.Id));

            QueueLeap(battle, assassin, leapTarget);
            Assert.That(battle.IsPioneerMovement, Is.False);
            Assert.That(battle.HasStartedPioneerMovement, Is.False);
            Assert.That(battle.IsAssassinLeap, Is.True);
            Assert.That(battle.HasStartedAssassinLeap, Is.False);
            battle.CancelAction();
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(assassin.ActionPoints, Is.EqualTo(8));
            Assert.That(assassin.IsInvisible, Is.True);

            QueueLeap(battle, assassin, leapTarget);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(assassin.ActionPoints, Is.EqualTo(5));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AttackAfterAnAmbushIsNotTreatedAsAnAmbush(bool cancelAmbush)
        {
            BattleSimulation battle = NewBattle(8);
            HexCharacter actor = SpawnAssassin(battle, _landing);
            HexCharacter target = SpawnEnemy(battle, _leapTargetOrigin);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryAmbush(actor.Id, target.Id), Is.True);

            if (cancelAmbush)
            {
                Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
                battle.CancelAction();
                Assert.That(battle.IsBusy, Is.False);
                Assert.That(actor.HasUsedAmbush, Is.False);
                Assert.That(actor.ActionPoints, Is.EqualTo(8));
            }
            else
            {
                Assert.That(battle.TryBeginAttack(out _), Is.True);
                Assert.That(battle.TryCompleteAttack(out _), Is.True);
                Assert.That(battle.IsBusy, Is.False);
                Assert.That(actor.HasUsedAmbush, Is.True);
                Assert.That(actor.IsInvisible, Is.False);
            }

            int actionPoints = actor.ActionPoints;
            int health = target.Health;
            Assert.That(battle.TryAttack(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(actor.HasUsedAmbush, Is.EqualTo(!cancelAmbush));
            Assert.That(actor.ActionPoints, Is.LessThan(actionPoints));
            Assert.That(target.Health, Is.EqualTo(health - result.Damage));
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void LeapExposesItsStateInEveryPhase()
        {
            BattleSimulation battle = NewBattle(3);
            HexCharacter actor = SpawnAssassin(battle, _assassinOrigin);
            HexCharacter target = SpawnEnemy(battle, _leapTargetOrigin);
            BattleTestUtility.StartTurn(battle, actor.Id);
            AssertSkillState(battle, BattleActionPhase.None, false, false, false, false);

            QueueLeap(battle, actor, target);
            AssertSkillState(battle, BattleActionPhase.Moving, false, false, true, false);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            AssertSkillState(battle, BattleActionPhase.Moving, false, false, true, true);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            AssertSkillState(battle, BattleActionPhase.ReadyToAttack, false, false, true, true);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            AssertSkillState(battle, BattleActionPhase.Attacking, false, false, true, true);
            Assert.That(battle.TryCompleteAttack(out _), Is.True);
            AssertSkillState(battle, BattleActionPhase.Settling, false, false, true, true);
            Assert.That(battle.TrySettleAction(), Is.True);
            AssertSkillState(battle, BattleActionPhase.None, false, false, false, false);
        }

        [Test]
        public void LeapStoppedBeforeIssueExposesAnUnstartedSettlingState()
        {
            BattleSimulation battle = NewBattle(3);
            HexCharacter actor = SpawnAssassin(battle, _assassinOrigin);
            HexCharacter target = SpawnEnemy(battle, _leapTargetOrigin);
            BattleTestUtility.StartTurn(battle, actor.Id);
            QueueLeap(battle, actor, target);
            battle.StopAction();
            AssertSkillState(battle, BattleActionPhase.Settling, false, false, true, false);
            Assert.That(actor.IsInvisible, Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(3));
            Assert.That(battle.TrySettleAction(), Is.True);
            AssertSkillState(battle, BattleActionPhase.None, false, false, false, false);
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void PioneerSkillExposesItsStateInEveryPhase(SkillType skill)
        {
            BattleSimulation battle = NewBattle(8);
            HexCharacter actor = SpawnPioneer(battle, _pioneerOrigin);
            HexCharacter target = SpawnEnemy(battle, _pullTargetOrigin);
            BattleTestUtility.StartTurn(battle, actor.Id);

            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill), Is.True);
            AssertSkillState(battle, BattleActionPhase.Moving, true, false, false, false);

            for (int i = 0; i < 3; i++)
            {
                Assert.That(battle.TryBeginMovementStep(out _), Is.True);
                AssertSkillState(battle, BattleActionPhase.Moving, true, true, false, false);
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
            }

            AssertSkillState(battle, BattleActionPhase.Settling, true, true, false, false);
            Assert.That(battle.TrySettleAction(), Is.True);
            AssertSkillState(battle, BattleActionPhase.None, false, false, false, false);

            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id,
                skill == SkillType.PullSelf ? SkillType.PullTarget : SkillType.PullSelf), Is.False);
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void PioneerSkillStoppedBeforeIssueExposesAnUnstartedSettlingState(SkillType skill)
        {
            BattleSimulation battle = NewBattle(8);
            HexCharacter actor = SpawnPioneer(battle, _pioneerOrigin);
            HexCharacter target = SpawnEnemy(battle, _pullTargetOrigin);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill), Is.True);
            battle.StopAction();
            AssertSkillState(battle, BattleActionPhase.Settling, true, false, false, false);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(battle.TrySettleAction(), Is.True);
            AssertSkillState(battle, BattleActionPhase.None, false, false, false, false);
        }

        static void AssertSkillState(BattleSimulation battle, BattleActionPhase phase, bool pioneer,
            bool pioneerStarted, bool leap, bool leapStarted)
        {
            Assert.That(battle.Phase, Is.EqualTo(phase));
            Assert.That(battle.IsPioneerMovement, Is.EqualTo(pioneer));
            Assert.That(battle.HasStartedPioneerMovement, Is.EqualTo(pioneerStarted));
            Assert.That(battle.IsAssassinLeap, Is.EqualTo(leap));
            Assert.That(battle.HasStartedAssassinLeap, Is.EqualTo(leapStarted));
        }

        static BattleSimulation NewBattle(int actionPoints)
        {
            return new BattleSimulation(12, 12, new BattleRules(startingActionPoints: actionPoints), 7);
        }

        static HexCharacter SpawnAssassin(BattleSimulation battle, HexCoordinate coordinate, int health = 20,
            int initiative = 30)
        {
            return Spawn(battle, BattleTeam.Player, coordinate, MercenaryClass.Rogue, MercenarySpecialization.Assassin,
                health, 3, initiative,
                new Trait(TraitType.RearGuaranteedCritical, new TraitSource(TraitSourceKind.Class, 0)),
                new Trait(TraitType.BattleStartInvisibility,
                    new TraitSource(TraitSourceKind.Specialization, (int)MercenarySpecialization.Assassin)));
        }

        static HexCharacter SpawnPioneer(BattleSimulation battle, HexCoordinate coordinate, int health = 20)
        {
            return Spawn(battle, BattleTeam.Player, coordinate, MercenaryClass.Warrior, MercenarySpecialization.Pioneer,
                health, 2, 30,
                new Trait(TraitType.SkillMovementRecovery,
                    new TraitSource(TraitSourceKind.Specialization, (int)MercenarySpecialization.Pioneer)));
        }

        static HexCharacter SpawnAlly(BattleSimulation battle, HexCoordinate coordinate)
        {
            return Spawn(battle, BattleTeam.Player, coordinate, MercenaryClass.Warrior, MercenarySpecialization.None,
                20, 2, 20);
        }

        static HexCharacter SpawnEnemy(BattleSimulation battle, HexCoordinate coordinate, int health = 20,
            int initiative = 10)
        {
            return Spawn(battle, BattleTeam.Enemy, coordinate, MercenaryClass.Warrior, MercenarySpecialization.None,
                health, 2, initiative);
        }

        static HexCharacter Spawn(BattleSimulation battle, BattleTeam team, HexCoordinate coordinate,
            MercenaryClass unitClass, MercenarySpecialization specialization, int health, int movement,
            int initiative, params Trait[] traits)
        {
            Assert.That(battle.TrySpawnUnit(team, coordinate,
                new BattleTestUnit(unitClass, health, 3, movement, 1, 0, initiative: initiative),
                HexDirection.East, out HexCharacter unit, new TraitCollection(traits),
                specialization: specialization), Is.True);
            return unit;
        }

        static void QueueLeap(BattleSimulation battle, HexCharacter actor, HexCharacter target)
        {
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out AssassinLeapPreview preview), Is.True);
            Assert.That(battle.TryAssassinLeap(actor.Id, target.Id, preview), Is.True);
        }

        static void FinishPioneerSkill(BattleSimulation battle)
        {
            for (int i = 0; i < 6 && battle.Phase == BattleActionPhase.Moving; i++)
            {
                Assert.That(battle.TryBeginMovementStep(out _), Is.True);
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.TrySettleAction(), Is.True);
        }

        static void Burn(BattleSimulation battle, HexCoordinate coordinate)
        {
            Assert.That(battle.TryApplyGroundEffect(coordinate, BattleGroundEffect.Burning, out _), Is.True);
        }

        static void MoveExternally(BattleSimulation battle, int unitId, HexCoordinate coordinate)
        {
            Assert.That((bool)battle.Board.GetType().GetMethod("TryRelocate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(battle.Board, new object[] { unitId, coordinate }), Is.True);
        }
    }
}
