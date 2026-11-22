using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRangeMeshViewTests
    {
        HexMapView _map;
        BattleRangeMeshView _view;
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _material = BattleTestViews.CreateRangeMaterial();
            _map = new GameObject("Map").AddComponent<HexMapView>();
            GameObject viewObject = new GameObject("Ranges");
            viewObject.transform.SetParent(_map.transform, false);
            _view = viewObject.AddComponent<BattleRangeMeshView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_map != null)
            {
                Object.DestroyImmediate(_map.gameObject);
            }

            if (_material != null)
            {
                Object.DestroyImmediate(_material);
            }
        }

        [Test]
        public void MovementWinsOverBothRepresentationsOfTheSameAttackEdge()
        {
            HexCoordinate first = new HexCoordinate(2, 2);
            HexCoordinate neighbor = first.GetNeighbor(HexDirection.East);
            BattleHexEdge movement = new BattleHexEdge(first, HexDirection.East);
            BattleHexEdge opposite = new BattleHexEdge(neighbor, HexDirection.West);
            Assert.That(_view.Rebuild(_map, new[] { movement, opposite },
                new[] { movement, opposite }, null, true, _material), Is.True);

            Mesh mesh = _view.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.triangles.Length, Is.EqualTo(6));
            Assert.That(mesh.GetTopology(0), Is.EqualTo(MeshTopology.Triangles));
            Assert.That(mesh.colors[0], Is.EqualTo(new Color(0.25f, 0.9f, 0.9f, 1f)));
            ValidateGeometry(mesh);
        }

        [Test]
        public void AttackEdgesAreDeduplicatedAndBecomeGrayWithoutAttackAp()
        {
            HexCoordinate first = new HexCoordinate(2, 2);
            BattleHexEdge edge = new BattleHexEdge(first, HexDirection.East);
            BattleHexEdge opposite = new BattleHexEdge(first.GetNeighbor(HexDirection.East), HexDirection.West);
            BattleGroundMarker[] markers = { new BattleGroundMarker(first, false) };
            Assert.That(_view.Rebuild(_map, null, new[] { edge, opposite }, markers, false, _material), Is.True);

            Mesh mesh = _view.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.colors[0], Is.EqualTo(new Color(0.67f, 0.67f, 0.67f, 1f)));
            Assert.That(_view.Rebuild(_map, null, new[] { edge }, null, true, _material), Is.True);
            Assert.That(mesh.colors[0], Is.EqualTo(new Color(1f, 0.44f, 0.44f, 1f)));
        }

        [TestCase(false, 24)]
        [TestCase(true, 48)]
        public void TargetsUseOneOrTwoSmallHexRings(bool requiresMovement, int vertexCount)
        {
            HexCoordinate coordinate = new HexCoordinate(3, 3);
            Assert.That(_view.Rebuild(_map, null, null,
                new[] { new BattleGroundMarker(coordinate, requiresMovement) }, true, _material), Is.True);

            Mesh mesh = _view.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(vertexCount));
            Color expected = requiresMovement ? new Color(1f, 0.7f, 0.25f, 1f) : new Color(1f, 0.31f, 0.31f, 1f);
            Assert.That(mesh.colors[0], Is.EqualTo(expected));
            Vector2 center = HexLayout.AxialToLocal(coordinate, _map.HexSize);
            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                float radius = Vector2.Distance(center, new Vector2(vertices[i].x, vertices[i].z));
                Assert.That(radius, Is.LessThan(_map.HexSize * 0.6f));
            }

            ValidateGeometry(mesh);
        }

        [Test]
        public void GroundGeometryUsesMapLocalCoordinatesAndHexRelativeWidth()
        {
            SetField(_map, "_hexSize", 2.5f);
            _map.transform.position = new Vector3(10f, 3f, -8f);
            _map.transform.rotation = Quaternion.Euler(0f, 37f, 0f);
            _map.transform.localScale = Vector3.one * 1.5f;
            BattleHexEdge edge = new BattleHexEdge(new HexCoordinate(2, 3), HexDirection.NorthEast);
            Assert.That(_view.Rebuild(_map, new[] { edge }, null, null, true, _material), Is.True);
            Mesh mesh = _view.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Assert.That(Vector3.Distance(vertices[0], vertices[3]), Is.EqualTo(0.05f * _map.HexSize).Within(0.00001f));
            BattleRangeOutline.GetLocalEdge(edge, _map.HexSize, out Vector3 start, out Vector3 end);
            Vector3 expectedCenter = (start + end) * 0.5f;
            expectedCenter.y = 0.02f;
            Vector3 actualCenter = (vertices[0] + vertices[1] + vertices[2] + vertices[3]) * 0.25f;
            Assert.That(Vector3.Distance(actualCenter, expectedCenter), Is.LessThan(0.00001f));
            Assert.That(Vector3.Distance(_view.transform.TransformPoint(actualCenter),
                _map.transform.TransformPoint(expectedCenter)), Is.LessThan(0.00001f));
            _map.transform.position += Vector3.right * 5f;
            Assert.That(_view.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            Assert.That(Vector3.Distance(_view.transform.TransformPoint(actualCenter),
                _map.transform.TransformPoint(expectedCenter)), Is.LessThan(0.00001f));
        }

        [Test]
        public void RepeatedHideShowAndRebuildKeepTheMeshAndMaterialAttached()
        {
            BattleHexEdge first = new BattleHexEdge(new HexCoordinate(1, 1), HexDirection.East);
            Assert.That(_view.Rebuild(_map, new[] { first }, null, null, true, _material), Is.True);
            MeshFilter filter = _view.GetComponent<MeshFilter>();
            MeshRenderer renderer = _view.GetComponent<MeshRenderer>();
            Mesh mesh = filter.sharedMesh;

            for (int i = 0; i < 4; i++)
            {
                _view.SetVisible(true);
                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.sharedMaterial, Is.SameAs(_material));
                Assert.That(filter.sharedMesh, Is.SameAs(mesh));
                _view.SetVisible(false);
                Assert.That(renderer.enabled, Is.False);
                Assert.That(mesh.vertexCount, Is.EqualTo(4));
                BattleHexEdge next = new BattleHexEdge(new HexCoordinate(i + 1, 2), HexDirection.NorthEast);
                Assert.That(_view.Rebuild(_map, new[] { next }, null, null, true, _material), Is.True);
                Assert.That(renderer.enabled, Is.False);
            }

            _view.SetVisible(true);
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(_material));
            Assert.That(filter.sharedMesh, Is.SameAs(mesh));
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(renderer.receiveShadows, Is.False);
            Assert.That(_view.GetComponent<Collider>(), Is.Null);
        }

        [Test]
        public void EmptyOrInvalidRebuildCannotDisplayThePreviousGeometry()
        {
            BattleHexEdge[] edges = { new BattleHexEdge(new HexCoordinate(1, 1), HexDirection.East) };
            Assert.That(_view.Rebuild(_map, edges, null, null, true, _material), Is.True);
            _view.SetVisible(true);
            Mesh mesh = _view.GetComponent<MeshFilter>().sharedMesh;
            MeshRenderer renderer = _view.GetComponent<MeshRenderer>();
            Assert.That(_view.Rebuild(_map, null, null, null, true, _material), Is.True);
            _view.SetVisible(true);
            Assert.That(renderer.enabled, Is.False);
            Assert.That(mesh.vertexCount, Is.Zero);
            Assert.That(_view.Rebuild(_map, edges, null, null, true, _material), Is.True);
            _view.SetVisible(true);
            Assert.That(_view.Rebuild(_map, edges, null, null, true, null), Is.False);
            _view.SetVisible(true);
            Assert.That(renderer.enabled, Is.False);
            Assert.That(mesh.vertexCount, Is.Zero);
            Assert.That(_view.Rebuild(_map, edges, null, null, true, _material), Is.True);
            _view.SetVisible(true);
            Assert.That(renderer.enabled, Is.True);
        }

        [TestCase(-1, 1, 0)]
        [TestCase(8, 1, 0)]
        [TestCase(1, 1, 6)]
        public void InvalidEdgesAreRejectedWithoutPartialGeometry(int q, int r, int direction)
        {
            BattleHexEdge[] edges =
            {
                new BattleHexEdge(new HexCoordinate(1, 1), HexDirection.East),
                new BattleHexEdge(new HexCoordinate(q, r), (HexDirection)direction)
            };
            Assert.That(_view.Rebuild(_map, edges, null, null, true, _material), Is.False);
            _view.SetVisible(true);
            Assert.That(_view.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        [TestCase(0f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.MaxValue)]
        public void InvalidOrOverflowingHexSizeDoesNotProduceNonFiniteVertices(float hexSize)
        {
            SetField(_map, "_hexSize", hexSize);
            BattleHexEdge[] edges = { new BattleHexEdge(new HexCoordinate(3, 3), HexDirection.East) };
            Assert.That(_view.Rebuild(_map, edges, null, null, true, _material), Is.False);
            _view.SetVisible(true);
            Assert.That(_view.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        [Test]
        public void LargeRangesUseThirtyTwoBitIndicesWithoutTruncation()
        {
            SetField(_map, "_width", 80);
            SetField(_map, "_height", 80);
            List<BattleHexEdge> edges = new List<BattleHexEdge>();

            for (int r = 0; r < 80; r++)
            {
                for (int q = 0; q < 80; q++)
                {
                    HexCoordinate coordinate = new HexCoordinate(q, r);
                    edges.Add(new BattleHexEdge(coordinate, HexDirection.East));
                    edges.Add(new BattleHexEdge(coordinate, HexDirection.NorthEast));
                    edges.Add(new BattleHexEdge(coordinate, HexDirection.NorthWest));
                }
            }

            Assert.That(_view.Rebuild(_map, edges, null, null, true, _material), Is.True);
            Mesh mesh = _view.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(edges.Count * 4));
            Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt32));
            ValidateGeometry(mesh);
            Assert.That(_view.Rebuild(_map, new[] { edges[0] }, null, null, true, _material), Is.True);
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt16));
        }

        [Test]
        public void DisablingAndDestroyingTheViewHideItAndReleaseOnlyItsOwnedMesh()
        {
            BattleHexEdge[] edges = { new BattleHexEdge(new HexCoordinate(1, 1), HexDirection.East) };
            Assert.That(_view.Rebuild(_map, edges, null, null, true, _material), Is.True);
            _view.SetVisible(true);
            Mesh mesh = _view.GetComponent<MeshFilter>().sharedMesh;
            MeshRenderer renderer = _view.GetComponent<MeshRenderer>();
            _view.enabled = false;
            Invoke(_view, "OnDisable");
            Assert.That(renderer.enabled, Is.False);
            _view.SetVisible(true);
            Assert.That(renderer.enabled, Is.False);
            _view.enabled = true;
            _view.SetVisible(true);
            Assert.That(renderer.enabled, Is.True);
            Invoke(_view, "OnDestroy");
            Object.DestroyImmediate(_map.gameObject);
            Assert.That(mesh == null, Is.True);
            Assert.That(_material != null, Is.True);
        }

        static void ValidateGeometry(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;
            Assert.That(mesh.colors.Length, Is.EqualTo(vertices.Length));

            for (int i = 0; i < vertices.Length; i++)
            {
                Assert.That(float.IsNaN(vertices[i].x) || float.IsInfinity(vertices[i].x), Is.False);
                Assert.That(float.IsNaN(vertices[i].z) || float.IsInfinity(vertices[i].z), Is.False);
                Assert.That(vertices[i].y, Is.GreaterThan(0.01f));
            }

            for (int i = 0; i < indices.Length; i++)
            {
                Assert.That(indices[i], Is.InRange(0, vertices.Length - 1));
            }
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
