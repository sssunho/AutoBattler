using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Animations;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSpiritImbuementRunnerTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        BattleRunner _runner;
        BattleSimulation _simulation;
        HexMapView _map;
        HexCharacter _actor;
        HexCharacter _middle;
        HexCharacter _target;
        Dictionary<int, HexCharacterView> _views;
        BattleAnimationFixture _animation;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 3);
            BattleSkillDefinition basic = new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit,
                SkillRangeSource.Own, 1, 6, 2, damage: 1);
            BattleSkillDefinition spirit = new BattleSkillDefinition(SkillType.SpiritImbuement, SkillTargetType.Self,
                SkillRangeSource.Own, 0, 0, 0);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                new BattleUnitStats(MercenaryClass.Archer, 10, 2, 0, initiative: 20), HexDirection.East,
                out _actor, skills: new SkillSet(new[] { basic, spirit }, new[] { 0, 3 })), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 1, 2, 0), HexDirection.West,
                out _middle, skills: new SkillSet(basic)), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 1, 2, 0), HexDirection.West,
                out _target, skills: new SkillSet(basic)), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            _views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            _views.Add(_actor.Id, CreateView(_actor));
            _views.Add(_middle.Id, CreateView(_middle));
            _views.Add(_target.Id, CreateView(_target));
        }

        [TearDown]
        public void TearDown()
        {
            _runner.ShutdownBattle();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }
            _objects.Clear();
            _animation?.Dispose();
            _animation = null;
        }

        [Test]
        public void ElectricBatchReleasesEveryDeadViewBeforePublishingBothResults()
        {
            HexCharacterView middleView = _views[_middle.Id];
            HexCharacterView targetView = _views[_target.Id];
            List<int> notified = new List<int>();
            _runner.AttackCompleted += (result, target, position) =>
            {
                Assert.That(_views.ContainsKey(_middle.Id), Is.False);
                Assert.That(_views.ContainsKey(_target.Id), Is.False);
                notified.Add(result.TargetId);
            };
            IssueElectricAttack();
            CompleteAttack();
            Assert.That(notified, Is.EquivalentTo(new[] { _middle.Id, _target.Id }));
            Assert.That(middleView.gameObject.activeSelf, Is.False);
            Assert.That(targetView.gameObject.activeSelf, Is.False);
            Assert.That(_actor.PreparedSpirit, Is.EqualTo(SpiritType.None));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CallbackShutdownOrDisableStopsRemainingBatchPresentation(bool disableAndReenable)
        {
            int notifications = 0;
            object driver = BattlePresentationTestUtility.GetDriver(_runner);
            _runner.AttackCompleted += (result, target, position) =>
            {
                notifications++;
                if (disableAndReenable)
                {
                    BattlePresentationTestUtility.DisableAndReenable(_runner);
                }
                else
                {
                    _runner.ShutdownBattle();
                }

                Assert.That(BattlePresentationTestUtility.GetDriver(_runner), Is.SameAs(driver));
                Assert.That(GetField(driver, "_isCompletingAttack"), Is.True);
            };
            IssueElectricAttack();
            CompleteAttack();
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(GetField(driver, "_isCompletingAttack"), Is.False);
            Assert.That(_middle.Health, Is.Zero);
            Assert.That(_target.Health, Is.Zero);
        }

        [Test]
        public void ReentrantCompletionCannotPublishOrApplyTheSameBatchAgain()
        {
            int notifications = 0;
            _runner.AttackCompleted += (result, target, position) =>
            {
                notifications++;
                CompleteAttack();
            };
            IssueElectricAttack();
            CompleteAttack();
            Assert.That(notifications, Is.EqualTo(2));
            Assert.That(_actor.ActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void MeleeHitDamagesBeforeAnimationEndsAndKeepsAttackBusy()
        {
            PrepareAnimatedAttack(AnimationAttackKind.Melee);
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 0f);
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 0.25f);
            Assert.That(_target.Health, Is.EqualTo(1));
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 0.25f);
            Assert.That(_target.Health, Is.Zero);
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 0.5f);
            Assert.That(_simulation.IsBusy, Is.False);
        }

        [Test]
        public void ProjectileWaitsForHitThenArrivalAfterAnimationCompletion()
        {
            PrepareAnimatedAttack(AnimationAttackKind.Projectile);
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 0f);
            object driver = BattlePresentationTestUtility.GetDriver(_runner);
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 0.25f);
            Assert.That(GetField(driver, "_projectile"), Is.Null);
            Assert.That(_target.Health, Is.EqualTo(1));
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 0.25f);
            Assert.That(GetField(driver, "_projectile"), Is.Not.Null);
            Assert.That(_target.Health, Is.EqualTo(1));
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 0.5f);
            Assert.That(_target.Health, Is.EqualTo(1));
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", 10f);
            Assert.That(_target.Health, Is.Zero);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(GetField(driver, "_projectile"), Is.Null);
        }

        void PrepareAnimatedAttack(AnimationAttackKind kind)
        {
            HexCharacterView view = _views[_actor.Id];
            GameObject model = CreateObject("Animated Model");
            model.transform.SetParent(view.transform, false);
            _animation = new BattleAnimationFixture(model, attackKind: kind);
            _animation.Attach(view);
            Assert.That(typeof(HexCharacterView).GetMethod("TryInitializeAnimation", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, null), Is.EqualTo(true));
            SetField(view, "_projectileSpeed", 1f);
            Assert.That(_simulation.TryAttack(_actor.Id, _target.Id), Is.True);
        }

        void IssueElectricAttack()
        {
            Assert.That(_simulation.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement,
                _actor.Id, spirit: SpiritType.Electricity)), Is.True);
            Assert.That(_simulation.TryAttack(_actor.Id, _target.Id), Is.True);
            Assert.That(_simulation.TryBeginAttack(out _), Is.True);
        }

        void CompleteAttack()
        {
            BattlePresentationTestUtility.Invoke(_runner, "ResolveDamage", true);
        }

        HexCharacterView CreateView(HexCharacter character)
        {
            HexCharacterView view = CreateObject("Unit").AddComponent<HexCharacterView>();
            SetField(view, "_character", character);
            SetField(view, "_mapView", _map);
            SetField(view, "_model", view.transform);
            view.transform.position = _map.AxialToWorld(character.Coordinate);
            return view;
        }

        GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
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
