using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleEnvironmentViewTests
    {
        readonly List<Object> _objects = new List<Object>();
        readonly HexCoordinate _coordinate = new HexCoordinate(2, 2);
        HexMapView _map;
        BattleRunner _runner;
        BattleEnvironmentView _view;
        BattleSimulation _simulation;
        Material _template;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _simulation = CreateSimulation();
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(shader, Is.Not.Null);
            _template = new Material(shader);
            _objects.Add(_template);
            _view = CreateObject("Environment View").AddComponent<BattleEnvironmentView>();
            SetField(_view, "_runner", _runner);
            SetField(_view, "_material", _template);
            Invoke(_view, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            if (_view != null)
            {
                Invoke(_view, "OnDisable");
            }

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void TwoSlotsHaveIndependentMeshesColorsAndNoColliders()
        {
            ApplyBoth();
            Invoke(_view, "LateUpdate");
            Assert.That(Markers.Count, Is.EqualTo(1));
            GameObject ground = Marker("_ground");
            GameObject air = Marker("_air");
            Assert.That(ground, Is.Not.Null);
            Assert.That(air, Is.Not.Null);
            Assert.That(ground.GetComponent<MeshFilter>().sharedMesh,
                Is.Not.SameAs(air.GetComponent<MeshFilter>().sharedMesh));
            Assert.That(ground.GetComponent<Collider>(), Is.Null);
            Assert.That(air.GetComponent<Collider>(), Is.Null);
            Assert.That(ground.GetComponent<MeshRenderer>().shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(air.GetComponent<MeshRenderer>().receiveShadows, Is.False);
            Assert.That(ground.transform.localPosition.y, Is.LessThan(air.transform.localPosition.y));
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            ground.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
            AssertColor(properties.GetColor("_BaseColor"), ReadField<Color>(_view, "_wetColor"));
            air.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
            AssertColor(properties.GetColor("_BaseColor"), ReadField<Color>(_view, "_smokeColor"));
        }

        [Test]
        public void ReapplyingStateKeepsObjectsAndClearingSlotsRemovesOnlyTheirMarkers()
        {
            ApplyBoth();
            Invoke(_view, "LateUpdate");
            GameObject ground = Marker("_ground");
            GameObject air = Marker("_air");
            Assert.That(_simulation.TryApplyGroundEffect(_coordinate, BattleGroundEffect.Wet, out _), Is.True);
            Invoke(_view, "LateUpdate");
            Assert.That(Marker("_ground"), Is.SameAs(ground));
            Assert.That(Marker("_air"), Is.SameAs(air));
            Assert.That(_simulation.TryClearEnvironment(_coordinate, true, false, out _), Is.True);
            Invoke(_view, "LateUpdate");
            Assert.That(ground == null, Is.True);
            Assert.That(Marker("_ground"), Is.Null);
            Assert.That(Marker("_air"), Is.SameAs(air));
            Assert.That(_simulation.TryClearEnvironment(_coordinate, false, true, out _), Is.True);
            Invoke(_view, "LateUpdate");
            Assert.That(air == null, Is.True);
            Assert.That(Markers.Count, Is.Zero);
            Assert.That(ReadField<GameObject>(_view, "_root"), Is.Null);
        }

        [Test]
        public void MapTransformsAndInspectorSettingsUpdateTheExistingMarkers()
        {
            ApplyBoth();
            Invoke(_view, "LateUpdate");
            GameObject ground = Marker("_ground");
            _map.transform.SetPositionAndRotation(new Vector3(5f, 2f, -3f), Quaternion.Euler(0f, 40f, 0f));
            _map.transform.localScale = Vector3.one * 2f;
            Vector2 center = HexLayout.AxialToLocal(_coordinate, _map.HexSize);
            Vector3 expected = _map.transform.TransformPoint(new Vector3(center.x, 0.06f * _map.HexSize, center.y));
            Assert.That(Vector3.Distance(ground.transform.position, expected), Is.LessThan(0.001f));
            SetField(_view, "_groundHeight", 0.2f);
            SetField(_view, "_wetColor", Color.magenta);
            Invoke(_view, "OnValidate");
            Invoke(_view, "LateUpdate");
            Assert.That(Marker("_ground"), Is.SameAs(ground));
            Assert.That(ground.transform.localPosition.y, Is.EqualTo(0.2f * _map.HexSize));
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            ground.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_BaseColor"), Is.EqualTo(Color.magenta));
        }

        [Test]
        public void DisablingViewReleasesItsResourcesButPreservesLogicalEnvironment()
        {
            ApplyBoth();
            Invoke(_view, "LateUpdate");
            GameObject root = ReadField<GameObject>(_view, "_root");
            Material runtime = ReadField<Material>(_view, "_runtimeMaterial");
            Assert.That(runtime, Is.Not.SameAs(_template));
            Color originalColor = _template.GetColor("_BaseColor");
            _view.enabled = false;
            Invoke(_view, "OnDisable");
            Assert.That(root == null, Is.True);
            Assert.That(runtime == null, Is.True);
            Assert.That(_template != null, Is.True);
            Assert.That(_template.GetColor("_BaseColor"), Is.EqualTo(originalColor));
            Assert.That(_simulation.Board.TryGetEnvironment(_coordinate, out BattleEnvironmentState state), Is.True);
            Assert.That(state.Ground, Is.EqualTo(BattleGroundEffect.Wet));
            _view.enabled = true;
            Invoke(_view, "OnEnable");
            Invoke(_view, "LateUpdate");
            Assert.That(Markers.Count, Is.EqualTo(1));
        }

        [Test]
        public void CompletedBattleKeepsEnvironmentVisibleAndNewBattleStartsWithoutMarkers()
        {
            ApplyBoth();
            Invoke(_view, "LateUpdate");
            GameObject ground = Marker("_ground");
            Assert.That(_simulation.RemoveUnit(2), Is.True);
            _simulation.TrySettleAction();
            Assert.That(_simulation.TryFinalizeResult(out _), Is.True);
            Invoke(_view, "LateUpdate");
            Assert.That(Marker("_ground"), Is.SameAs(ground));
            SetField(_runner, "_simulation", CreateSimulation());
            Invoke(_view, "LateUpdate");
            Assert.That(ground == null, Is.True);
            Assert.That(Markers.Count, Is.Zero);
        }

        [Test]
        public void EnemyTurnAndBusyActionDoNotHideTheEnvironment()
        {
            ApplyBoth();
            Invoke(_view, "LateUpdate");
            GameObject ground = Marker("_ground");
            Assert.That(_simulation.TryEndTurn(1), Is.True);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(2));
            Assert.That(_simulation.TryMove(2, new HexCoordinate(_map.Width - 2, _map.Height - 1)), Is.True);
            Assert.That(_simulation.IsBusy, Is.True);
            Invoke(_view, "LateUpdate");
            Assert.That(Marker("_ground"), Is.SameAs(ground));
            Assert.That(ground.activeInHierarchy, Is.True);
        }

        [Test]
        public void PresentationStopAndMapDisableRemoveAllMarkers()
        {
            ApplyBoth();
            Invoke(_view, "LateUpdate");
            GameObject first = ReadField<GameObject>(_view, "_root");
            ReadField<System.Action>(_runner, "PresentationStopped").Invoke();
            Assert.That(first == null, Is.True);
            Invoke(_view, "LateUpdate");
            Assert.That(Markers.Count, Is.EqualTo(1));
            GameObject second = ReadField<GameObject>(_view, "_root");
            _map.enabled = false;
            Invoke(_view, "LateUpdate");
            Assert.That(second == null, Is.True);
            Assert.That(Markers.Count, Is.Zero);
        }

        [Test]
        public void RebindingRunnerRemovesOldMarkersAndSubscribesOnlyOnce()
        {
            ApplyBoth();
            Invoke(_view, "LateUpdate");
            Invoke(_view, "LateUpdate");
            Assert.That(ReadField<System.Action>(_runner, "PresentationStopped").GetInvocationList().Length, Is.EqualTo(1));
            BattleRunner next = CreateObject("Next Runner").AddComponent<BattleRunner>();
            SetField(next, "_simulation", CreateSimulation());
            SetField(next, "_mapView", _map);
            SetField(next, "_hexSize", _map.HexSize);
            SetField(_view, "_runner", next);
            Invoke(_view, "LateUpdate");
            Assert.That(Markers.Count, Is.Zero);
            Assert.That(ReadField<System.Action>(_runner, "PresentationStopped"), Is.Null);
            Assert.That(ReadField<System.Action>(next, "PresentationStopped").GetInvocationList().Length, Is.EqualTo(1));
        }

        IDictionary Markers => ReadField<IDictionary>(_view, "_markers");

        GameObject Marker(string field)
        {
            return ReadField<GameObject>(Markers[_coordinate], field);
        }

        void ApplyBoth()
        {
            Assert.That(_simulation.TryApplyGroundEffect(_coordinate, BattleGroundEffect.Wet, out string error), Is.True, error);
            Assert.That(_simulation.TryApplyAirEffect(_coordinate, BattleAirEffect.Smoke, out error), Is.True, error);
        }

        BattleSimulation CreateSimulation()
        {
            BattleSimulation simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 17);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 0),
                new BattleTestUnit(MercenaryClass.Warrior, 10, 3, 3, 1, 0, initiative: 20), HexDirection.East, out _), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(_map.Width - 1, _map.Height - 1),
                new BattleTestUnit(MercenaryClass.Warrior, 10, 3, 3, 1, 0, initiative: 10), HexDirection.West, out _), Is.True);
            Assert.That(simulation.TryStartBattle(), Is.True);
            return simulation;
        }

        GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        static T ReadField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }

        static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
    }
}
