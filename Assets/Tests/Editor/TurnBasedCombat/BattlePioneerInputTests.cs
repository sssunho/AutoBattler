using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattlePioneerInputTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        Material _material;
        HexMapView _map;
        Camera _camera;
        BattleRunner _runner;
        BattleInputController _input;
        BattleStatusView _status;
        Button _endTurn;
        BattleSimulation _simulation;
        HexCharacter _pioneer;
        HexCharacter _enemy;
        BattleRangeController _ranges;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _camera = CreateObject("Camera").AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 10f;
            _camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            _camera.transform.position = new Vector3(0f, 10f, 0f);
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", _camera);
            SetField(_input, "_characterLayerMask", (LayerMask)0);
            _status = CreateObject("Status").AddComponent<BattleStatusView>();
            _endTurn = CreateObject("End Turn", typeof(RectTransform)).AddComponent<Button>();
            SetField(_status, "_runner", _runner);
            SetField(_status, "_endTurnButton", _endTurn);
            _simulation = new BattleSimulation(_map.Width, _map.Height,
                new BattleRules(startingActionPoints: 4), 7);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 20, 3, 5, 1, 0, initiative: 10),
                HexDirection.East, out _pioneer,
                new TraitCollection(new[] { new Trait(TraitType.SkillMovementRecovery,
                    new TraitSource(TraitSourceKind.Specialization, (int)MercenarySpecialization.Pioneer)) }),
                specialization: MercenarySpecialization.Pioneer), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(5, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 20, 2, 3, 1, 0),
                HexDirection.East, out _enemy), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Add(_pioneer.Id, CreateView(_pioneer));
            views.Add(_enemy.Id, CreateView(_enemy));
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
            for (int i = 0; i < _animations.Count; i++)
            {
                _animations[i].Dispose();
            }

            _animations.Clear();

            if (_material != null)
            {
                Object.DestroyImmediate(_material);
            }
        }

        [TestCase(SkillType.BasicAttack)]
        [TestCase(SkillType.Ambush)]
        [TestCase(SkillType.Overwatch)]
        [TestCase((SkillType)(-1))]
        [TestCase((SkillType)99)]
        public void PioneerSelectionRejectsOtherSkillTypes(SkillType skill)
        {
            Assert.That(_input.TryBeginPioneerSelection(skill), Is.False);
            Assert.That(_input.IsSelectingPioneerSkill(skill), Is.False);
            Assert.That(_input.IsSelectingAction, Is.False);
            Assert.That(_runner.IsSelectingAction, Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(4));
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void SelectionLocksCommandsAndCancelsWithoutSpending(SkillType skill)
        {
            Assert.That(_input.TryBeginPioneerSelection(skill), Is.True);
            Assert.That(_input.IsSelectingPioneerSkill(skill), Is.True);
            Assert.That(_runner.IsSelectingAction, Is.True);
            Assert.That(_input.TryBeginStealthSelection(), Is.False);
            Assert.That(_input.TryBeginPioneerSelection(skill), Is.False);
            Assert.That(_runner.TryMove(_pioneer.Id, new HexCoordinate(1, 2)), Is.False);
            Assert.That(_runner.TryAttack(_pioneer.Id, _enemy.Id), Is.False);
            Assert.That(_runner.TryEndTurn(_pioneer.Id), Is.False);
            Invoke(_status, "Refresh");
            Assert.That(_endTurn.interactable, Is.False);
            _endTurn.onClick.Invoke();
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_pioneer.Id));
            _runner.CancelAction();
            Assert.That(_runner.IsSelectingAction, Is.False);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(4));
            Assert.That(_pioneer.MovementRemaining, Is.EqualTo(5));
            Assert.That(_pioneer.HasUsedSkillMovementRecovery, Is.False);
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void ConfirmingTargetQueuesTheCorrectMoverWithoutSpendingEarly(SkillType skill)
        {
            _map.transform.SetPositionAndRotation(new Vector3(1f, 0f, 2f), Quaternion.Euler(0f, 27f, 0f));
            _map.transform.localScale = new Vector3(1.3f, 1f, 0.8f);
            Assert.That(_input.TryBeginPioneerSelection(skill), Is.True);
            Assert.That(ConfirmCoordinate(_pioneer.Coordinate), Is.False);
            Assert.That(ConfirmCoordinate(_enemy.Coordinate), Is.True);
            Assert.That(_input.IsSelectingAction, Is.False);
            Assert.That(_simulation.ActingUnitId, Is.EqualTo(_pioneer.Id));
            Assert.That(_simulation.MovingUnitId, Is.EqualTo(skill == SkillType.PullSelf ? _pioneer.Id : _enemy.Id));
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(4));
            Assert.That(_pioneer.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(_runner.TryConfirmPioneerSkill(_input, _simulation, _pioneer.Id, _enemy.Id, skill), Is.False);
        }

        [Test]
        public void WrongSkillCannotConfirmAnotherSelection()
        {
            Assert.That(_input.TryBeginPioneerSelection(SkillType.PullSelf), Is.True);
            Assert.That(_runner.TryConfirmPioneerSkill(_input, _simulation, _pioneer.Id,
                _enemy.Id, SkillType.PullTarget), Is.False);
            Assert.That(_input.IsSelectingPioneerSkill(SkillType.PullSelf), Is.True);
            Assert.That(_simulation.IsBusy, Is.False);
        }

        [TestCase("camera")]
        [TestCase("input")]
        [TestCase("round")]
        [TestCase("replacement")]
        [TestCase("shutdown")]
        public void InvalidSelectionContextCannotCommit(string reason)
        {
            Assert.That(_input.TryBeginPioneerSelection(SkillType.PullTarget), Is.True);
            Assert.That(Hover(_enemy.Coordinate), Is.True);

            switch (reason)
            {
                case "camera":
                    _camera.enabled = false;
                    break;
                case "input":
                    _input.enabled = false;
                    Invoke(_input, "OnDisable");
                    break;
                case "round":
                    Assert.That(_simulation.TryEndTurn(_pioneer.Id), Is.True);
                    Assert.That(_simulation.TryEndTurn(_enemy.Id), Is.True);
                    break;
                case "replacement":
                    SetField(_runner, "_simulation", new BattleSimulation(_map.Width, _map.Height));
                    break;
                default:
                    _runner.ShutdownBattle();
                    break;
            }

            Assert.That(_input.IsSelectingAction, Is.False);
            Assert.That(_input.TryGetPioneerContext(_runner, out _, out _, out _, out _), Is.False);
            Assert.That(_runner.TryConfirmPioneerSkill(_input, _simulation, _pioneer.Id,
                _enemy.Id, SkillType.PullTarget), Is.False);
        }

        [TestCase(SkillType.PullSelf, 4)]
        [TestCase(SkillType.PullTarget, 3)]
        public void HoverShowsMoverPathAndDistinctLanding(SkillType skill, int landingQ)
        {
            CreateRanges();
            Invoke(_ranges, "LateUpdate");
            BattleRangeMeshView normal = ((BattleRangePresentationLayer)GetField(_ranges, "_rangeLayer")).View;
            Assert.That(_input.TryBeginPioneerSelection(skill), Is.True);
            Assert.That(Hover(_enemy.Coordinate), Is.True);
            Invoke(_ranges, "LateUpdate");
            BattleRangeMeshView view = ((BattleRangePresentationLayer)GetField(_ranges, "_pioneerLayer")).View;
            Mesh mesh = view.GetComponent<MeshFilter>().sharedMesh;
            List<HexCoordinate> path = (List<HexCoordinate>)GetField(_ranges, "_pioneerPath");
            List<HexCoordinate> landing = (List<HexCoordinate>)GetField(_ranges, "_pioneerLanding");
            Assert.That(path.Count, Is.EqualTo(2));
            Assert.That(landing[0], Is.EqualTo(new HexCoordinate(landingQ, 2)));
            Assert.That(mesh.vertexCount, Is.EqualTo(36));
            Assert.That(mesh.colors[0], Is.EqualTo(new Color(1f, 0.9f, 0.2f, 0.65f)));
            Assert.That(mesh.colors[18], Is.EqualTo(new Color(0.15f, 0.75f, 1f, 0.4f)));
            Assert.That(normal.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(4));
            Assert.That(Hover(new HexCoordinate(0, 0)), Is.False);
            Invoke(_ranges, "LateUpdate");
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(Hover(_enemy.Coordinate), Is.True);
            Invoke(_ranges, "LateUpdate");
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void PreviewIsRemovedWhenSelectedTargetDisappearsOrSelectionEnds()
        {
            CreateRanges();
            Assert.That(_input.TryBeginPioneerSelection(SkillType.PullTarget), Is.True);
            Assert.That(Hover(_enemy.Coordinate), Is.True);
            Invoke(_ranges, "LateUpdate");
            BattleRangeMeshView view = ((BattleRangePresentationLayer)GetField(_ranges, "_pioneerLayer")).View;
            Assert.That(_runner.RemoveCharacter(_enemy.Id), Is.True);
            Invoke(_ranges, "LateUpdate");
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            _runner.ShutdownBattle();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            _ranges.enabled = false;
            Invoke(_ranges, "OnDisable");
            Assert.That(view == null, Is.True);
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void MouseGroundPointChoosesThePreviewAndCommittedPath(SkillType skill)
        {
            MethodInfo move = _simulation.Board.GetType().GetMethod("TryMove",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That((bool)move.Invoke(_simulation.Board, new object[] { _enemy.Id, new HexCoordinate(5, 3) }), Is.True);
            Assert.That((bool)move.Invoke(_simulation.Board, new object[] { _enemy.Id, new HexCoordinate(4, 4) }), Is.True);
            _map.transform.SetPositionAndRotation(new Vector3(2f, 0f, 3f), Quaternion.Euler(0f, 31f, 0f));
            _map.transform.localScale = new Vector3(1.3f, 1f, 0.8f);
            CreateRanges();
            Assert.That(_input.TryBeginPioneerSelection(skill), Is.True);
            Vector2 center = HexLayout.AxialToLocal(_enemy.Coordinate, _map.HexSize);
            Vector3 firstScreen = _camera.WorldToScreenPoint(_map.transform.TransformPoint(
                new Vector3(center.x, 0f, center.y + _map.HexSize * 0.35f)));
            Vector3 secondScreen = _camera.WorldToScreenPoint(_map.transform.TransformPoint(
                new Vector3(center.x, 0f, center.y - _map.HexSize * 0.35f)));
            Assert.That(_input.TryUpdatePioneerTarget(new Vector2(firstScreen.x, firstScreen.y)), Is.True);
            Assert.That(_input.TryGetPioneerPath(_runner, _pioneer.Id, _enemy.Id, skill,
                out IReadOnlyList<HexCoordinate> first), Is.True);
            List<HexCoordinate> expected = new List<HexCoordinate>(first);
            Invoke(_ranges, "LateUpdate");
            Assert.That((List<HexCoordinate>)GetField(_ranges, "_pioneerPath"), Is.EqualTo(expected));

            Assert.That(_input.TryUpdatePioneerTarget(new Vector2(secondScreen.x, secondScreen.y)), Is.True);
            Assert.That(_input.TryGetPioneerPath(_runner, _pioneer.Id, _enemy.Id, skill,
                out IReadOnlyList<HexCoordinate> second), Is.True);
            Assert.That(second, Is.Not.EqualTo(expected));
            expected = new List<HexCoordinate>(second);
            Assert.That(_input.TryConfirmSelectedPosition(new Vector2(firstScreen.x, firstScreen.y)), Is.True);
            Assert.That(_input.IsSelectingAction, Is.False);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.That(_simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
                Assert.That(step.To, Is.EqualTo(expected[i]));
                Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(_simulation.TrySettleAction(), Is.True);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(2));
        }

        [TestCase("parallel")]
        [TestCase("center")]
        [TestCase("nonfinite")]
        public void InvalidGroundAimClearsThePreviewAndCannotCommit(string reason)
        {
            CreateRanges();
            Assert.That(_input.TryBeginPioneerSelection(SkillType.PullSelf), Is.True);
            Assert.That(Hover(_enemy.Coordinate), Is.True);
            Invoke(_ranges, "LateUpdate");
            BattleRangeMeshView view = ((BattleRangePresentationLayer)GetField(_ranges, "_pioneerLayer")).View;
            Vector3 screen = _camera.WorldToScreenPoint(_map.AxialToWorld(_pioneer.Coordinate));
            Vector2 position = new Vector2(screen.x, screen.y);

            if (reason == "parallel")
            {
                _camera.transform.rotation = Quaternion.identity;
            }
            else if (reason == "nonfinite")
            {
                position = new Vector2(float.NaN, 0f);
            }

            Assert.That(_input.TryUpdatePioneerTarget(position), Is.False);
            Invoke(_ranges, "LateUpdate");
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(_input.TryGetPioneerPath(_runner, _pioneer.Id, _enemy.Id, SkillType.PullSelf, out _), Is.False);
            Assert.That(_runner.TryConfirmPioneerSkill(_input, _simulation, _pioneer.Id, _enemy.Id,
                SkillType.PullSelf), Is.False);
            Assert.That(_input.TryConfirmSelectedPosition(position), Is.False);
            Assert.That(_pioneer.ActionPoints, Is.EqualTo(4));
            Assert.That(_simulation.IsBusy, Is.False);
        }

        [Test]
        public void CancelledPreviewCannotConfirmAfterStartingAnotherSelection()
        {
            Assert.That(_input.TryBeginPioneerSelection(SkillType.PullSelf), Is.True);
            Assert.That(Hover(_enemy.Coordinate), Is.True);
            _input.CancelActionSelection();
            Assert.That(_input.TryBeginPioneerSelection(SkillType.PullSelf), Is.True);
            Assert.That(_runner.TryConfirmPioneerSkill(_input, _simulation, _pioneer.Id, _enemy.Id,
                SkillType.PullSelf), Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
        }

        void CreateRanges()
        {
            _material = BattleTestViews.CreateRangeMaterial();
            _ranges = CreateObject("Ranges").AddComponent<BattleRangeController>();
            SetField(_ranges, "_runner", _runner);
            SetField(_ranges, "_input", _input);
            SetField(_ranges, "_material", _material);
        }

        bool Hover(HexCoordinate coordinate)
        {
            Vector3 screen = _camera.WorldToScreenPoint(_map.AxialToWorld(coordinate));
            return _input.TryUpdatePioneerTarget(new Vector2(screen.x, screen.y));
        }

        bool ConfirmCoordinate(HexCoordinate coordinate)
        {
            Vector3 screen = _camera.WorldToScreenPoint(_map.AxialToWorld(coordinate));
            _input.TryUpdateSelectedPosition(new Vector2(screen.x, screen.y));
            return _input.TryConfirmSelectedPosition(new Vector2(screen.x, screen.y));
        }

        HexCharacterView CreateView(HexCharacter character)
        {
            GameObject gameObject = CreateObject("Character");
            HexCharacterView view = gameObject.AddComponent<HexCharacterView>();
            BattleAnimationFixture animation = new BattleAnimationFixture(gameObject);
            _animations.Add(animation);
            animation.Attach(view);
            Assert.That(view.TryBind(character, _map), Is.True);
            view.transform.position = _map.AxialToWorld(character.Coordinate);
            view.ApplyFacing();
            Assert.That(view.IsReady, Is.True);
            return view;
        }

        GameObject CreateObject(string name, params System.Type[] components)
        {
            GameObject instance = new GameObject(name, components);
            _objects.Add(instance);
            return instance;
        }

        static object Invoke(object target, string name)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
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
