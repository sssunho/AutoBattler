using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRunnerReactionTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        BattleRunner _runner;
        BattleSimulation _simulation;
        HexMapView _map;
        HexCharacter _mover;
        HexCharacter _firstReactor;
        HexCharacter _secondReactor;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 3);
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Class, 0))
            });
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                CreateStats(30), HexDirection.East, out _mover), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 2),
                CreateStats(20), HexDirection.West, out _firstReactor, traits), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 1),
                CreateStats(10), HexDirection.West, out _secondReactor, traits), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_mover.Id));
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            Dictionary<int, HexCharacterView> views = GetViews();
            views.Add(_mover.Id, CreateView(_mover));
            views.Add(_firstReactor.Id, CreateView(_firstReactor));
            views.Add(_secondReactor.Id, CreateView(_secondReactor));
            Assert.That(_simulation.TryMove(_mover.Id, new HexCoordinate(3, 2)), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
        }

        [TearDown]
        public void TearDown()
        {
            if (_runner != null)
            {
                _runner.ShutdownBattle();
            }

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            for (int i = _animations.Count - 1; i >= 0; i--)
            {
                _animations[i].Dispose();
            }

            _animations.Clear();
        }

        [Test]
        public void CancelCannotDiscardReservedOrIssuedReactions()
        {
            _runner.CancelAction();
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
            Tick();
            Assert.That(_simulation.ActingUnitId, Is.EqualTo(_firstReactor.Id));
            _runner.CancelAction();
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));

            CompletePlayingAttack();
            Tick();
            CompletePlayingAttack();
            Tick();

            Assert.That(_mover.Health, Is.EqualTo(16));
            Assert.That(_mover.Coordinate, Is.EqualTo(new HexCoordinate(2, 2)));
            Assert.That(_mover.MovementRemaining, Is.EqualTo(3));
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(GetField(_runner, "_failedEnemyUnitId"), Is.EqualTo(0));
            Assert.That(_firstReactor.ActionPoints, Is.EqualTo(4));
            Assert.That(_secondReactor.ActionPoints, Is.EqualTo(4));
        }

        [Test]
        public void ReentrantUpdateCannotDispatchNextReactionInsideAttackCallback()
        {
            int notifications = 0;
            _runner.AttackCompleted += (result, target, position) =>
            {
                notifications++;
                int health = _mover.Health;
                Tick();
                Assert.That(_mover.Health, Is.EqualTo(health));
                Assert.That(_runner.IsBusy, Is.True);
            };

            Tick();

            Assert.That(notifications, Is.Zero);
            CompletePlayingAttack();
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(_mover.Health, Is.EqualTo(18));
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
            Tick();
            CompletePlayingAttack();
            Assert.That(notifications, Is.EqualTo(2));
            Assert.That(_mover.Health, Is.EqualTo(16));
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Tick();
            Assert.That(_runner.IsBusy, Is.False);
        }

        [Test]
        public void ReactionPresentationUsesReactorViewInsteadOfMoverView()
        {
            HexCharacterView moverView = GetViews()[_mover.Id];
            HexCharacterView reactorView = GetViews()[_firstReactor.Id];
            Quaternion moverRotation = Quaternion.Euler(0f, 23f, 0f);
            moverView.transform.rotation = moverRotation;
            reactorView.transform.rotation = Quaternion.Euler(0f, 51f, 0f);

            Tick();

            Vector3 expectedFacing = _map.AxialToWorld(_mover.Coordinate)
                - _map.AxialToWorld(_firstReactor.Coordinate);
            Assert.That(Vector3.Angle(reactorView.transform.forward, expectedFacing), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(moverView.transform.rotation, moverRotation), Is.LessThan(0.1f));
        }

        [Test]
        public void AttackCallbackShutdownDiscardsRemainingReactions()
        {
            int notifications = 0;
            _runner.AttackCompleted += (result, target, position) =>
            {
                notifications++;
                _runner.ShutdownBattle();
            };

            Tick();
            CompletePlayingAttack();
            Tick();

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(_mover.Health, Is.EqualTo(18));
            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_simulation.TryBeginAttack(out _), Is.False);
        }

        [Test]
        public void ShutdownAbortsReactionBeforeHitWithoutDamageOrPublishingPresentation()
        {
            int notifications = 0;
            _runner.AttackCompleted += (result, target, position) => notifications++;
            Tick();
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(_mover.Health, Is.EqualTo(20));

            _runner.ShutdownBattle();
            _runner.ShutdownBattle();

            Assert.That(_mover.Health, Is.EqualTo(20));
            Assert.That(notifications, Is.Zero);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(_simulation.TryBeginAttack(out _), Is.False);
        }

        [Test]
        public void DisableAndReenableInCallbackDoesNotResumeDiscardedBatch()
        {
            int notifications = 0;
            _runner.AttackCompleted += (result, target, position) =>
            {
                notifications++;
                BattlePresentationTestUtility.DisableAndReenable(_runner);
            };

            Tick();
            CompletePlayingAttack();
            Tick();

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(_mover.Health, Is.EqualTo(18));
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_runner.CurrentUnitId, Is.EqualTo(_mover.Id));
        }

        [Test]
        public void AttackerRemovalAbortsIssuedReactionButAllowsTheNextReservedReaction()
        {
            int notifications = 0;
            _runner.AttackCompleted += (result, target, position) => notifications++;
            Tick();
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(_runner.RemoveCharacter(_firstReactor.Id), Is.True);

            Tick();

            Assert.That(_mover.Health, Is.EqualTo(20));
            Assert.That(notifications, Is.Zero);
            Assert.That(GetField(_runner, "_failedEnemyUnitId"), Is.EqualTo(0));
            Tick();
            CompletePlayingAttack();
            Assert.That(_mover.Health, Is.EqualTo(18));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void RemovingMoverClearsOldMovementPresentationBeforeNextTurn()
        {
            _simulation.StopAction();
            Assert.That(_simulation.TrySettleAction(), Is.True);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep", true);
            Assert.That(_runner.RemoveCharacter(_mover.Id), Is.True);

            Tick();

            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
        }

        static BattleTestUnit CreateStats(int initiative)
        {
            return new BattleTestUnit(MercenaryClass.Warrior, 20, 2, 3, 1, 0, initiative: initiative);
        }

        HexCharacterView CreateView(HexCharacter character)
        {
            HexCharacterView view = CreateObject("Unit").AddComponent<HexCharacterView>();
            BattleAnimationFixture animation = new BattleAnimationFixture(view.gameObject);
            _animations.Add(animation);
            animation.Attach(view);
            Assert.That(view.TryBind(character, _map), Is.True);
            view.transform.position = _map.AxialToWorld(character.Coordinate);
            return view;
        }

        void CompletePlayingAttack()
        {
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            BattlePresentationTestUtility.AdvanceAnimations(_runner, 1f);
            Tick();
        }

        Dictionary<int, HexCharacterView> GetViews()
        {
            return (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
        }

        GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        void Tick()
        {
            typeof(BattleRunner).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_runner, null);
        }

        static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
