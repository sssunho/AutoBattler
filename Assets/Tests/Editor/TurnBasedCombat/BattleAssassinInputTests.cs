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
    public sealed class BattleAssassinInputTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        HexMapView _map;
        Camera _camera;
        BattleRunner _runner;
        BattleInputController _input;
        BattleSimulation _simulation;
        HexCharacter _assassin;
        HexCharacter _enemy;
        BattleRangeController _ranges;
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _camera = CreateObject("Camera").AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 12f;
            _camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            _camera.transform.position = new Vector3(0f, 10f, 0f);
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", _camera);
            SetField(_input, "_characterLayerMask", (LayerMask)0);
            _simulation = new BattleSimulation(_map.Width, _map.Height, new BattleRules(startingActionPoints: 6), 7);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                new BattleTestUnit(MercenaryClass.Rogue, 20, 3, 5, 1, 0, initiative: 20), HexDirection.East,
                out _assassin, specialization: MercenarySpecialization.Assassin), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(5, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 20, 2, 3, 1, 0), HexDirection.East, out _enemy), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            CreateView(_assassin);
            CreateView(_enemy);
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

            if (_material != null)
            {
                Object.DestroyImmediate(_material);
            }
        }

        [Test]
        public void InvisibilitySelectionLocksCommandsAndConfirmsOnlyTheOwner()
        {
            Assert.That(_input.TryBeginInvisibilitySelection(), Is.True);
            Assert.That(_runner.TryMove(_assassin.Id, new HexCoordinate(1, 2)), Is.False);
            Assert.That(_runner.TryEndTurn(_assassin.Id), Is.False);
            Assert.That(_input.TryBeginAssassinLeapSelection(), Is.False);
            Assert.That(_input.TryConfirmSelectedPosition(Screen(_enemy.Coordinate)), Is.False);
            _runner.CancelAction();
            Assert.That(_assassin.ActionPoints, Is.EqualTo(6));
            Assert.That(_assassin.IsInvisible, Is.False);
            Assert.That(_input.TryBeginInvisibilitySelection(), Is.True);
            Assert.That(_input.TryConfirmSelectedPosition(Screen(_assassin.Coordinate)), Is.True);
            Assert.That(_assassin.IsInvisible, Is.True);
            Assert.That(_assassin.ActionPoints, Is.EqualTo(4));
            Assert.That(_assassin.MovementRemaining, Is.EqualTo(5));
            Assert.That(_input.IsSelectingAction, Is.False);
            Assert.That(_input.TryBeginInvisibilitySelection(), Is.False);
        }

        [Test]
        public void LeapPreviewShowsLandingAndConfirmationKeepsTheSnapshot()
        {
            CreateRanges();
            Assert.That(_input.TryBeginAssassinLeapSelection(), Is.True);
            Assert.That(_input.TryUpdateAssassinLeapTarget(Screen(_enemy.Coordinate)), Is.True);
            Assert.That(_input.TryGetAssassinLeapContext(_runner, out _, out _, out AssassinLeapPreview preview), Is.True);
            Assert.That(preview.Landing, Is.EqualTo(new HexCoordinate(4, 2)));
            Invoke(_ranges, "LateUpdate");
            BattleRangeMeshView view = ((BattleRangePresentationLayer)GetField(_ranges, "_assassinLayer")).View;
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(view.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(18));
            Assert.That(view.GetComponent<Canvas>(), Is.Null);
            Assert.That(_input.TryConfirmSelectedPosition(Screen(_enemy.Coordinate)), Is.True);
            Assert.That(_input.IsSelectingAction, Is.False);
            Assert.That(_assassin.ActionPoints, Is.EqualTo(6));
            Assert.That(_simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(preview.Landing));
            Assert.That(_assassin.ActionPoints, Is.EqualTo(3));
            Invoke(_ranges, "LateUpdate");
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        [TestCase("facing")]
        [TestCase("position")]
        public void ClickingAfterTheTargetChangesRejectsTheCachedLandingWithoutSpending(string change)
        {
            Assert.That(_input.TryBeginAssassinLeapSelection(), Is.True);
            Assert.That(_input.TryUpdateAssassinLeapTarget(Screen(_enemy.Coordinate)), Is.True);
            Assert.That(_input.TryGetAssassinLeapContext(_runner, out _, out _, out AssassinLeapPreview preview), Is.True);
            if (change == "facing")
            {
                typeof(HexCharacter).GetMethod("SetFacing", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_enemy, new object[] { HexDirection.West, false });
            }
            else
            {
                Assert.That((bool)_simulation.Board.GetType().GetMethod("TryMove",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_simulation.Board,
                    new object[] { _enemy.Id, new HexCoordinate(5, 3) }), Is.True);
            }

            Assert.That(_input.TryConfirmSelectedPosition(Screen(_enemy.Coordinate)), Is.False);
            Assert.That(_input.TryGetAssassinLeapContext(_runner, out _, out _, out _), Is.False);
            Assert.That(_runner.TryConfirmAssassinLeap(_input, _simulation, _assassin.Id, _enemy.Id, preview), Is.False);
            Assert.That(_assassin.ActionPoints, Is.EqualTo(6));
            Assert.That(_simulation.IsBusy, Is.False);
        }

        [Test]
        public void ClickingWithoutALandingPreviewCannotQueueALeap()
        {
            Assert.That(_input.TryBeginAssassinLeapSelection(), Is.True);
            Assert.That(_input.TryConfirmSelectedPosition(Screen(_enemy.Coordinate)), Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_assassin.ActionPoints, Is.EqualTo(6));
        }

        [Test]
        public void InvalidHoverAndCancellationRemoveTheLandingPreview()
        {
            CreateRanges();
            Assert.That(_input.TryBeginAssassinLeapSelection(), Is.True);
            Assert.That(_input.TryUpdateAssassinLeapTarget(Screen(_enemy.Coordinate)), Is.True);
            Invoke(_ranges, "LateUpdate");
            BattleRangeMeshView view = ((BattleRangePresentationLayer)GetField(_ranges, "_assassinLayer")).View;
            Assert.That(_input.TryUpdateAssassinLeapTarget(new Vector2(float.NaN, 0f)), Is.False);
            Invoke(_ranges, "LateUpdate");
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(_input.TryUpdateAssassinLeapTarget(Screen(_enemy.Coordinate)), Is.True);
            _runner.CancelAction();
            Invoke(_ranges, "LateUpdate");
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(_input.TryBeginAssassinLeapSelection(), Is.True);
            Assert.That(_input.TryGetAssassinLeapContext(_runner, out _, out _, out _), Is.False);
            _ranges.enabled = false;
            Invoke(_ranges, "OnDisable");
            Assert.That(view == null, Is.True);
        }

        [Test]
        public void InvisibleAmbushPreviewHasNoDetectionDangerAndHudDistinguishesTheState()
        {
            CreateRanges();
            Assert.That(_simulation.TryEnterInvisibility(_assassin.Id), Is.True);
            Assert.That(_input.TryGetStealthContext(_runner, out _, out _), Is.False);
            Assert.That(_input.TryBeginAmbushSelection(), Is.True);
            Invoke(_ranges, "LateUpdate");
            Assert.That((List<HexCoordinate>)GetField(_ranges, "_detectionCells"), Is.Empty);
            _input.CancelActionSelection();
            BattleStatusView status = CreateObject("Status").AddComponent<BattleStatusView>();
            Text text = CreateObject("Text", typeof(RectTransform)).AddComponent<Text>();
            SetField(status, "_runner", _runner);
            SetField(status, "_statusText", text);
            Invoke(status, "Refresh");
            Assert.That(text.text, Does.Contain("투명화 중"));
            Assert.That(text.text, Does.Contain("3 후방 도약"));
            Assert.That(text.text, Does.Contain("4 투명화"));
        }

        [Test]
        public void ConcealmentMarkerDistinguishesInvisibilityFromStealth()
        {
            GameObject root = CreateObject("Overlay", typeof(RectTransform));
            HexCharacterOverlayView view = root.AddComponent<HexCharacterOverlayView>();
            RectTransform health = (RectTransform)CreateObject("Health", typeof(RectTransform)).transform;
            health.SetParent(root.transform, false);
            Image template = CreateObject("Template", typeof(RectTransform)).AddComponent<Image>();
            template.transform.SetParent(health, false);
            Image marker = CreateObject("Marker", typeof(RectTransform)).AddComponent<Image>();
            marker.transform.SetParent(root.transform, false);
            SetField(view, "_healthRoot", health);
            SetField(view, "_healthSegmentTemplate", template);
            SetField(view, "_turnMarker", marker);
            Assert.That(view.TryInitialize(3), Is.True);
            view.SetStealthStatus(true);
            Image concealment = (Image)GetField(view, "_stealthMarker");
            Color stealthColor = concealment.color;
            view.SetStealthStatus(true, true);
            Assert.That(concealment.color, Is.Not.EqualTo(stealthColor));
            Assert.That(concealment.raycastTarget, Is.False);
            view.SetStealthStatus(false);
            Assert.That(concealment.gameObject.activeSelf, Is.False);
        }

        void CreateRanges()
        {
            _material = BattleTestViews.CreateRangeMaterial();
            _ranges = CreateObject("Ranges").AddComponent<BattleRangeController>();
            SetField(_ranges, "_runner", _runner);
            SetField(_ranges, "_input", _input);
            SetField(_ranges, "_material", _material);
        }

        Vector2 Screen(HexCoordinate coordinate)
        {
            Vector3 screen = _camera.WorldToScreenPoint(_map.AxialToWorld(coordinate));
            return new Vector2(screen.x, screen.y);
        }

        void CreateView(HexCharacter character)
        {
            HexCharacterView view = BattleTestViews.CreateCharacterView("Unit " + character.Id, _objects, _animations);
            Assert.That(view.TryBind(character, _map), Is.True);
            ((Dictionary<int, HexCharacterView>)GetField(_runner, "_views")).Add(character.Id, view);
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
