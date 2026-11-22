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
    public sealed class BattleOverwatchRangeTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        HexMapView _map;
        BattleRunner _runner;
        BattleInputController _input;
        BattleRangeController _controller;
        BattleSimulation _simulation;
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
            _simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 3);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            _controller = CreateObject("Range Controller").AddComponent<BattleRangeController>();
            SetField(_controller, "_runner", _runner);
            SetField(_controller, "_input", _input);
            SetField(_controller, "_material", _material);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controller != null)
            {
                Object.DestroyImmediate(_controller.gameObject);
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

            if (_material != null)
            {
                Object.DestroyImmediate(_material);
            }
        }

        [Test]
        public void FilledHexesDeduplicateOverlapsAndGivePreviewItsOwnColor()
        {
            BattleRangeMeshView view = CreateObject("Filled Ranges").AddComponent<BattleRangeMeshView>();
            HexCoordinate first = new HexCoordinate(2, 2);
            HexCoordinate second = new HexCoordinate(3, 2);
            Assert.That(view.RebuildFilledHexes(_map, new[] { first, first, second },
                new[] { first, first }, _material), Is.True);
            view.SetVisible(true);
            Mesh mesh = view.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(36));
            Assert.That(mesh.triangles.Length, Is.EqualTo(36));
            Assert.That(mesh.colors[0], Is.EqualTo(new Color(1f, 0.9f, 0.2f, 0.48f)));
            Assert.That(mesh.colors[18], Is.EqualTo(new Color(1f, 0.5f, 0.12f, 0.32f)));
            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i += 3)
            {
                Assert.That(vertices[i].y, Is.GreaterThan(0f));
                Assert.That(Vector3.Cross(vertices[i + 1] - vertices[i], vertices[i + 2] - vertices[i]).sqrMagnitude,
                    Is.GreaterThan(0f));
            }

            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(view.GetComponent<Canvas>(), Is.Null);
            Assert.That(view.GetComponent<Collider>(), Is.Null);
        }

        [Test]
        public void InvalidOrEmptyFilledRangeCannotRevealPreviousGeometry()
        {
            BattleRangeMeshView view = CreateObject("Filled Ranges").AddComponent<BattleRangeMeshView>();
            Assert.That(view.RebuildFilledHexes(_map, new[] { new HexCoordinate(2, 2) }, null, _material), Is.True);
            view.SetVisible(true);
            Mesh mesh = view.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(view.RebuildFilledHexes(_map, new[] { new HexCoordinate(-1, 2) }, null, _material), Is.False);
            view.SetVisible(true);
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(mesh.vertexCount, Is.Zero);
            Assert.That(view.RebuildFilledHexes(_map, null, null, _material), Is.True);
            view.SetVisible(true);
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        [Test]
        public void ConfirmedRangeStaysVisibleDuringEnemyTurnAndMovement()
        {
            HexCharacter archer = Spawn(BattleTeam.Player, 2, 2, 20);
            HexCharacter enemy = Spawn(BattleTeam.Enemy, 6, 6, 10);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            Assert.That(_simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(enemy.Id));
            Tick();
            BattleRangeMeshView view = GetOverwatchView();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.transform.parent, Is.EqualTo(_map.transform));
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Mesh mesh = view.GetComponent<MeshFilter>().sharedMesh;
            int vertices = mesh.vertexCount;
            Assert.That(vertices, Is.GreaterThan(0));

            Assert.That(_simulation.TryMove(enemy.Id, new HexCoordinate(5, 6)), Is.True);
            Tick();

            Assert.That(_simulation.IsBusy, Is.True);
            Assert.That(GetOverwatchView(), Is.SameAs(view));
            Assert.That(view.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            Assert.That(mesh.vertexCount, Is.EqualTo(vertices));
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void MultipleArcherRangesMergeAndOnlyTheStartingArchersRangeExpires()
        {
            HexCharacter first = Spawn(BattleTeam.Player, 2, 2, 30);
            HexCharacter second = Spawn(BattleTeam.Player, 2, 3, 20);
            HexCharacter enemy = Spawn(BattleTeam.Enemy, 6, 6, 10);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            Assert.That(_simulation.TryPrepareOverwatch(first.Id, HexDirection.East), Is.True);
            Assert.That(_simulation.TryPrepareOverwatch(second.Id, HexDirection.East), Is.True);
            Tick();
            Mesh mesh = GetOverwatchView().GetComponent<MeshFilter>().sharedMesh;
            HashSet<HexCoordinate> union = new HashSet<HexCoordinate>();
            List<HexCoordinate> cells = new List<HexCoordinate>();
            BattleOverwatchArea.CopyCells(_map.Width, _map.Height, first.Coordinate, HexDirection.East, 3, cells);
            union.UnionWith(cells);
            BattleOverwatchArea.CopyCells(_map.Width, _map.Height, second.Coordinate, HexDirection.East, 3, cells);
            union.UnionWith(cells);
            Assert.That(mesh.vertexCount, Is.EqualTo(union.Count * 18));

            Assert.That(_simulation.TryEndTurn(enemy.Id), Is.True);
            Tick();

            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(first.Id));
            Assert.That(mesh.vertexCount, Is.EqualTo(cells.Count * 18));
            Assert.That(GetOverwatchView().GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void ReplacementBattleAndShutdownHideOldConfirmedRanges()
        {
            HexCharacter archer = Spawn(BattleTeam.Player, 2, 2, 20);
            Spawn(BattleTeam.Enemy, 6, 6, 10);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            Assert.That(_simulation.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Tick();
            BattleRangeMeshView view = GetOverwatchView();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            SetField(_runner, "_simulation", new BattleSimulation(_map.Width, _map.Height));
            Tick();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);

            SetField(_runner, "_simulation", _simulation);
            Tick();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            _runner.ShutdownBattle();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
            _controller.enabled = false;
            Invoke(_controller, "OnDisable");
            Assert.That(view == null, Is.True);
        }

        HexCharacter Spawn(BattleTeam team, int q, int r, int initiative)
        {
            Assert.That(_simulation.TrySpawnUnit(team, new HexCoordinate(q, r),
                new BattleTestUnit(MercenaryClass.Archer, 10, 2, 3, 3, 0, initiative: initiative),
                HexDirection.East, out HexCharacter unit), Is.True);
            return unit;
        }

        BattleRangeMeshView GetOverwatchView()
        {
            return ((BattleRangePresentationLayer)typeof(BattleRangeController)
                .GetField("_overwatchLayer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controller)).View;
        }

        void Tick()
        {
            typeof(BattleRangeController).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_controller, null);
        }

        GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
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
