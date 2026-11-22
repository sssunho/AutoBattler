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
    public sealed class BattleStealthRangeTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        HexMapView _map;
        BattleRunner _runner;
        BattleInputController _input;
        BattleRangeController _ranges;
        BattleSimulation _simulation;
        HexCharacter _rogue;
        HexCharacter _enemy;
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _material = BattleTestViews.CreateRangeMaterial();
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", camera);
            _simulation = new BattleSimulation(_map.Width, _map.Height,
                new BattleRules(startingActionPoints: 4), 3);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                new BattleTestUnit(MercenaryClass.Rogue, 20, 2, 4, 1, 0, initiative: 20),
                HexDirection.East, out _rogue), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 20, 2, 3, 1, 0),
                HexDirection.East, out _enemy), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Add(_rogue.Id, CreateReadyView(_rogue));
            views.Add(_enemy.Id, CreateReadyView(_enemy));
            _ranges = CreateObject("Ranges").AddComponent<BattleRangeController>();
            SetField(_ranges, "_runner", _runner);
            SetField(_ranges, "_input", _input);
            SetField(_ranges, "_material", _material);
        }

        [TearDown]
        public void TearDown()
        {
            if (_ranges != null)
            {
                Object.DestroyImmediate(_ranges.gameObject);
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

        [Test]
        public void StealthPreparationShowsDetectionAndOwnCellWithoutSpendingResources()
        {
            Assert.That(_input.TryBeginStealthSelection(), Is.True);
            Tick();
            BattleRangeMeshView view = GetStealthView();
            Mesh mesh = view.GetComponent<MeshFilter>().sharedMesh;
            List<HexCoordinate> cells = new List<HexCoordinate>();
            BattleDetectionArea.CopyCells(_map.Width, _map.Height, _enemy.Coordinate, _enemy.Facing, cells);
            Assert.That(mesh.vertexCount, Is.EqualTo((cells.Count + 1) * 18));
            Assert.That(mesh.colors[0], Is.EqualTo(new Color(0.25f, 1f, 0.65f, 0.5f)));
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(_rogue.IsStealthed, Is.False);
            Assert.That(_rogue.ActionPoints, Is.EqualTo(4));
            Assert.That(_rogue.MovementRemaining, Is.EqualTo(4));
        }

        [Test]
        public void DetectedUnitCannotBeginStealthSelection()
        {
            typeof(HexCharacter).GetMethod("SetFacing", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_enemy, new object[] { HexDirection.West, false });
            Assert.That(_input.TryBeginStealthSelection(), Is.False);
            Tick();
            Assert.That(_input.IsSelectingStealth, Is.False);
            Assert.That(_runner.TryConfirmStealth(_input, _simulation, _rogue.Id), Is.False);
            Assert.That(_rogue.IsStealthed, Is.False);
            Assert.That(_rogue.ActionPoints, Is.EqualTo(4));
        }

        [Test]
        public void DetectionReturnsAfterMovementAndHidesAfterAttackFinishes()
        {
            Assert.That(_simulation.TryEnterStealth(_rogue.Id), Is.True);
            Tick();
            BattleRangeMeshView view = GetStealthView();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(_simulation.TryMove(_rogue.Id, new HexCoordinate(1, 2)), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.True);
            Tick();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            Tick();
            Assert.That(GetStealthView(), Is.SameAs(view));
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(_simulation.TryMove(_rogue.Id, new HexCoordinate(2, 2)), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(_simulation.TryAttack(_rogue.Id, _enemy.Id), Is.True);
            Assert.That(_simulation.TryBeginAttack(out _), Is.True);
            Assert.That(_simulation.TryCompleteAttack(out _), Is.True);
            Tick();
            Assert.That(_rogue.IsStealthed, Is.False);
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        [Test]
        public void AmbushSelectionHighlightsOnlyEligibleTargetAndKeepsNormalRange()
        {
            Tick();
            BattleRangeMeshView normal = ((BattleRangePresentationLayer)GetField(_ranges, "_rangeLayer")).View;
            Assert.That(normal, Is.Not.Null);
            Assert.That(_simulation.TryEnterStealth(_rogue.Id), Is.True);
            Assert.That(_simulation.CanAmbush(_rogue.Id, _enemy.Id), Is.True);
            Assert.That(_input.TryBeginAmbushSelection(), Is.True);
            Tick();
            Assert.That(normal.GetComponent<MeshRenderer>().enabled, Is.True);
            Mesh mesh = GetStealthView().GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.colors[0], Is.EqualTo(new Color(0.7f, 0.4f, 1f, 0.6f)));
            List<HexCoordinate> markers = (List<HexCoordinate>)GetField(_ranges, "_stealthMarkers");
            Assert.That(markers.Count, Is.EqualTo(1));
            Assert.That(markers[0], Is.EqualTo(_enemy.Coordinate));
        }

        [Test]
        public void ShutdownAndDisableRemoveDetectionPresentation()
        {
            Assert.That(_simulation.TryEnterStealth(_rogue.Id), Is.True);
            Tick();
            BattleRangeMeshView view = GetStealthView();
            _runner.ShutdownBattle();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            _ranges.enabled = false;
            Invoke(_ranges, "OnDisable");
            Assert.That(view == null, Is.True);
        }

        [Test]
        public void StealthMarkerReusesItsObjectAndDoesNotInterceptClicks()
        {
            GameObject root = CreateObject("Overlay", typeof(RectTransform));
            HexCharacterOverlayView overlay = root.AddComponent<HexCharacterOverlayView>();
            GameObject health = CreateObject("Health", typeof(RectTransform));
            health.transform.SetParent(root.transform, false);
            Image template = CreateObject("Template", typeof(RectTransform)).AddComponent<Image>();
            template.transform.SetParent(health.transform, false);
            Image turn = CreateObject("Turn", typeof(RectTransform)).AddComponent<Image>();
            turn.transform.SetParent(root.transform, false);
            SetField(overlay, "_healthRoot", health.transform);
            SetField(overlay, "_healthSegmentTemplate", template);
            SetField(overlay, "_turnMarker", turn);
            Assert.That(overlay.TryInitialize(5), Is.True);
            overlay.SetStealthStatus(true);
            Image marker = (Image)GetField(overlay, "_stealthMarker");
            Assert.That(marker.raycastTarget, Is.False);
            Assert.That(marker.gameObject.activeSelf, Is.True);
            overlay.SetStealthStatus(false);
            Assert.That(marker.gameObject.activeSelf, Is.False);
            overlay.SetStealthStatus(true);
            Assert.That(GetField(overlay, "_stealthMarker"), Is.SameAs(marker));
            Assert.That(marker.gameObject.activeSelf, Is.True);
        }

        HexCharacterView CreateReadyView(HexCharacter character)
        {
            GameObject gameObject = CreateObject("Character");
            HexCharacterView view = gameObject.AddComponent<HexCharacterView>();
            BattleAnimationFixture animation = new BattleAnimationFixture(gameObject);
            _animations.Add(animation);
            animation.Attach(view);
            Assert.That(view.TryBind(character, _map), Is.True);
            Assert.That(view.IsReady, Is.True);
            return view;
        }

        BattleRangeMeshView GetStealthView()
        {
            return ((BattleRangePresentationLayer)GetField(_ranges, "_stealthLayer")).View;
        }

        void Tick()
        {
            typeof(BattleRangeController).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_ranges, null);
        }

        GameObject CreateObject(string name, params System.Type[] components)
        {
            GameObject gameObject = new GameObject(name, components);
            _objects.Add(gameObject);
            return gameObject;
        }

        static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
    }
}
