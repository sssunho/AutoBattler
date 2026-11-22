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
    public sealed class BattleAssassinRunnerTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        BattleRunner _runner;
        BattleInputController _input;
        HexMapView _map;
        BattleSimulation _simulation;
        HexCharacter _assassin;
        HexCharacter _target;
        HexCharacterView _assassinView;
        HexCharacterView _targetView;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", camera);
            _simulation = new BattleSimulation(_map.Width, _map.Height,
                new BattleRules(startingActionPoints: 3), 9);
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.BattleStartInvisibility,
                    new TraitSource(TraitSourceKind.Specialization, (int)MercenarySpecialization.Assassin)),
                new Trait(TraitType.RearGuaranteedCritical, new TraitSource(TraitSourceKind.Class, 0))
            });
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 5),
                new BattleTestUnit(MercenaryClass.Rogue, 30, 3, 2, 1, 0, initiative: 30),
                HexDirection.East, out _assassin, traits, specialization: MercenarySpecialization.Assassin), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(6, 5),
                new BattleTestUnit(MercenaryClass.Warrior, 30, 3, 2, 1, 0, initiative: 10),
                HexDirection.East, out _target), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            _assassinView = CreateView(_assassin);
            _targetView = CreateView(_target);
        }

        [TearDown]
        public void TearDown()
        {
            _input.CancelActionSelection();
            _runner.ShutdownBattle();

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            BattleTestViews.DisposeFixtures(_animations);
        }

        [Test]
        public void LeapPresentsAnArcAndIssuesThePrepaidAttackAtZeroAp()
        {
            QueueLeap();
            Tick(0.01f);
            Assert.That(_assassin.ActionPoints, Is.Zero);
            Assert.That(_assassin.Coordinate, Is.EqualTo(new HexCoordinate(5, 5)));
            Assert.That(_assassinView.transform.position.y, Is.GreaterThan(_map.transform.position.y));
            Assert.That(_assassin.IsInvisible, Is.True);
            Assert.That(_assassin.MovementRemaining, Is.EqualTo(2));
            _runner.CancelAction();
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(_runner.PauseMovement(), Is.False);
            Tick(100f);
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(_assassin.ActionPoints, Is.Zero);
            Tick(100f);
            Tick(0f);
            Assert.That(_target.Health, Is.EqualTo(25));
            Assert.That(_assassin.IsStealthed, Is.False);
            Assert.That(_assassin.HasUsedAmbush, Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TargetDisableBeforeOrAfterCommitHasTheCorrectCostAndLanding(bool started)
        {
            QueueLeap();

            if (started)
            {
                Tick(0.01f);
            }

            _targetView.enabled = false;
            Tick(0.01f);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_assassin.ActionPoints, Is.EqualTo(started ? 0 : 3));
            Assert.That(_assassin.Coordinate, Is.EqualTo(new HexCoordinate(started ? 5 : 2, 5)));
            Assert.That(_assassin.IsInvisible, Is.EqualTo(!started));
            Assert.That(_target.Health, Is.EqualTo(30));
            Assert.That(_assassinView.IsPresenting, Is.False);
            Assert.That(Vector3.Distance(_assassinView.transform.position, _map.AxialToWorld(_assassin.Coordinate)),
                Is.LessThan(0.001f));
        }

        [Test]
        public void RunnerDisableAfterCommitSettlesWithoutIssuingTheAttack()
        {
            QueueLeap();
            Tick(0.01f);
            _runner.enabled = false;
            Invoke(_runner, "OnDisable");
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_assassin.Coordinate, Is.EqualTo(new HexCoordinate(5, 5)));
            Assert.That(_assassin.ActionPoints, Is.Zero);
            Assert.That(_assassin.IsStealthed, Is.False);
            Assert.That(_target.Health, Is.EqualTo(30));
            Assert.That(_assassinView.IsPresenting, Is.False);
        }

        [Test]
        public void RunnerDisableBeforeHitAbortsTheIssuedAttackWithoutDamage()
        {
            QueueLeap();
            Tick(100f);
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(_target.Health, Is.EqualTo(30));
            _runner.enabled = false;
            Invoke(_runner, "OnDisable");
            Assert.That(_target.Health, Is.EqualTo(30));
            Assert.That(_assassin.ActionPoints, Is.Zero);
            Assert.That(_assassin.IsStealthed, Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
        }

        void QueueLeap()
        {
            Assert.That(_input.TryBeginAssassinLeapSelection(), Is.True);
            Assert.That((bool)Invoke(_input, "UpdateSelectionPreview",
                new SkillInputQuery(SkillType.AssassinLeap, _assassin.Id, _target.Id)), Is.True);
            Assert.That(_simulation.TryGetAssassinLeapPreview(_assassin.Id, _target.Id,
                out AssassinLeapPreview preview), Is.True);
            Assert.That(_runner.TryConfirmAssassinLeap(_input, _simulation, _assassin.Id, _target.Id, preview), Is.True);
        }

        void Tick(float deltaTime)
        {
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", deltaTime);
        }

        HexCharacterView CreateView(HexCharacter character)
        {
            HexCharacterView view = BattleTestViews.CreateCharacterView("Unit " + character.Id, _objects, _animations);
            Assert.That(view.TryBind(character, _map), Is.True);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)_runner.GetType()
                .GetField("_views", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_runner);
            views.Add(character.Id, view);
            return view;
        }

        GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        static object Invoke(object target, string name, params object[] arguments)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, arguments);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
