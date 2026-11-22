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
    public sealed class BattlePioneerRunnerTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        BattleRunner _runner;
        BattleInputController _input;
        HexMapView _map;
        BattleSimulation _simulation;
        HexCharacter _pioneer;
        HexCharacter _target;
        HexCharacter _ally;
        HexCharacterView _pioneerView;
        HexCharacterView _targetView;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            camera.transform.position = new Vector3(0f, 10f, 0f);
            camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", camera);
            SetField(_input, "_characterLayerMask", (LayerMask)0);
            _simulation = new BattleSimulation(_map.Width, _map.Height,
                new BattleRules(startingActionPoints: 6), 9);
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.SkillMovementRecovery,
                    new TraitSource(TraitSourceKind.Specialization, (int)MercenarySpecialization.Pioneer))
            });
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 2),
                Stats(30), HexDirection.West, out _pioneer, traits,
                specialization: MercenarySpecialization.Pioneer), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(5, 2),
                Stats(10), HexDirection.East, out _target), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 4),
                Stats(20), HexDirection.East, out _ally), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            _pioneerView = CreateView(_pioneer);
            _targetView = CreateView(_target);
            CreateView(_ally);
        }

        [TearDown]
        public void TearDown()
        {
            if (_input != null)
            {
                _input.CancelActionSelection();
            }

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
            BattleTestViews.DisposeFixtures(_animations);
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void PresentsTheActualMoverWithoutAttackingOrAdvancingTheTurn(SkillType skill)
        {
            Queue(skill);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(6));
            Assert.That(_simulation.MovingUnitId,
                Is.EqualTo(skill == SkillType.PullSelf ? _pioneer.Id : _target.Id));
            Tick(10f);
            Tick(0f);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(4));
            Assert.That(_pioneer.MovementRemaining, Is.EqualTo(2));
            Assert.That(_target.MovementRemaining, Is.Zero);
            Assert.That(_pioneer.HasUsedSkillMovementRecovery, Is.True);
            Assert.That(_pioneer.Coordinate, Is.EqualTo(new HexCoordinate(skill == SkillType.PullSelf ? 4 : 1, 2)));
            Assert.That(_target.Coordinate, Is.EqualTo(new HexCoordinate(skill == SkillType.PullSelf ? 5 : 2, 2)));
            Assert.That(_target.Health, Is.EqualTo(30));
            Assert.That(_target.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_pioneer.Id));
            Assert.That(Vector3.Distance(_targetView.transform.position, _map.AxialToWorld(_target.Coordinate)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(_pioneerView.transform.position, _map.AxialToWorld(_pioneer.Coordinate)), Is.LessThan(0.001f));
            Assert.That(GetField(_runner, "_failedEnemyUnitId"), Is.EqualTo(0));
        }

        [Test]
        public void UserCancelCannotStopAnIssuedPullOrResetItsPresentation()
        {
            Queue(SkillType.PullTarget);
            Tick(0.01f);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            _runner.CancelAction();
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(_targetView.IsPresenting, Is.True);
            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.EqualTo(true));
            Tick(10f);
            Tick(0f);
            Assert.That(_target.Coordinate, Is.EqualTo(new HexCoordinate(2, 2)));
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(4));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LosingAViewBeforeFirstCommitDoesNotSpend(bool actor)
        {
            Queue(SkillType.PullTarget);
            (actor ? _pioneerView : _targetView).enabled = false;
            Tick(0.01f);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(6));
            Assert.That(_pioneer.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(_target.Coordinate, Is.EqualTo(new HexCoordinate(5, 2)));
        }

        [TestCase("runner")]
        [TestCase("actor-view")]
        [TestCase("target-view")]
        public void InterruptedPullSettlesOnlyCommittedStepAndRevealsCaster(string reason)
        {
            Assert.That(_simulation.TryEnterStealth(_pioneer.Id), Is.True);
            Queue(SkillType.PullTarget);
            Tick(0.01f);
            Assert.That(_target.Coordinate, Is.EqualTo(new HexCoordinate(4, 2)));

            if (reason == "runner")
            {
                _runner.enabled = false;
                Invoke(_runner, "OnDisable");
            }
            else
            {
                (reason == "actor-view" ? _pioneerView : _targetView).enabled = false;
                Tick(0.01f);
            }

            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_pioneer.IsStealthed, Is.False);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(2));
            Assert.That(_pioneer.MovementRemaining, Is.EqualTo(1));
            Assert.That(_target.Coordinate, Is.EqualTo(new HexCoordinate(4, 2)));
            Assert.That(Vector3.Distance(_targetView.transform.position, _map.AxialToWorld(_target.Coordinate)), Is.LessThan(0.001f));
            Assert.That(_targetView.IsPresenting, Is.False);
            Assert.That(GetField(_runner, "_failedEnemyUnitId"), Is.EqualTo(0));
        }

        [Test]
        public void RemovingCasterPreservesThePulledUnitsCommittedStepAndAdvancesOnce()
        {
            Queue(SkillType.PullTarget);
            Tick(0.01f);
            Assert.That(_runner.RemoveCharacter(_pioneer.Id), Is.True);
            Tick(0.01f);
            Tick(0f);
            Assert.That(_target.Coordinate, Is.EqualTo(new HexCoordinate(4, 2)));
            Assert.That(Vector3.Distance(_targetView.transform.position, _map.AxialToWorld(_target.Coordinate)), Is.LessThan(0.001f));
            Assert.That(_targetView.IsPresenting, Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_ally.Id));
            Tick(0f);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_ally.Id));
        }

        void Queue(SkillType skill)
        {
            Assert.That(_input.TryBeginPioneerSelection(skill), Is.True);
            Camera camera = (Camera)GetField(_input, "_inputCamera");
            Vector3 screen = camera.WorldToScreenPoint(_map.AxialToWorld(_target.Coordinate));
            Assert.That(_input.TryUpdatePioneerTarget(new Vector2(screen.x, screen.y)), Is.True);
            Assert.That(_runner.TryConfirmPioneerSkill(_input, _simulation, _pioneer.Id, _target.Id, skill), Is.True);
        }

        void Tick(float deltaTime)
        {
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", deltaTime);
        }

        HexCharacterView CreateView(HexCharacter character)
        {
            HexCharacterView view = BattleTestViews.CreateCharacterView("Unit " + character.Id, _objects, _animations);
            Assert.That(view.TryBind(character, _map), Is.True);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Add(character.Id, view);
            return view;
        }

        static BattleTestUnit Stats(int initiative)
        {
            return new BattleTestUnit(MercenaryClass.Warrior, 30, 3, 2, 1, 0, initiative: initiative);
        }

        GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        static object Invoke(object target, string name, params object[] arguments)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
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
