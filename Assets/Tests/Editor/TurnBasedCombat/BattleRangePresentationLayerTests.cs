using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRangePresentationLayerTests
    {
        BattleRangePresentationLayer _layer;
        HexMapView _map;
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _material = BattleTestViews.CreateRangeMaterial();
            _layer = new BattleRangePresentationLayer("Test Range");
            _map = new GameObject("Test Map").AddComponent<HexMapView>();
        }

        [TearDown]
        public void TearDown()
        {
            _layer.Release();
            Object.DestroyImmediate(_map.gameObject);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void DestroyedViewIsRebuiltEvenWhenCellsAndMaterialAreUnchanged()
        {
            HexCoordinate[] cells = { new HexCoordinate(0, 0) };
            Assert.That(_layer.ShowFilled(_map, cells, cells, _material, Color.red, Color.blue), Is.True);
            GameObject previousObject = _layer.View.gameObject;
            Object.DestroyImmediate(_layer.View);
            Assert.That(_layer.ShowFilled(_map, cells, cells, _material, Color.red, Color.blue), Is.True);
            Assert.That(previousObject == null, Is.True);
            Assert.That(_layer.View.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
            Assert.That(_layer.View.GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void PreviewAndColorChangesRebuildTheFilledDisplay()
        {
            HexCoordinate[] cells = { new HexCoordinate(0, 0), new HexCoordinate(1, 0) };
            HexCoordinate[] preview = { cells[0] };
            Assert.That(_layer.ShowFilled(_map, cells, preview, _material, Color.red, Color.blue), Is.True);
            Mesh mesh = _layer.View.GetComponent<MeshFilter>().sharedMesh;
            Vector3 firstVertex = mesh.vertices[0];
            preview[0] = cells[1];
            Assert.That(_layer.ShowFilled(_map, cells, preview, _material, Color.red, Color.green), Is.True);
            Assert.That(mesh.vertices[0], Is.Not.EqualTo(firstVertex));
            Assert.That(mesh.colors[0], Is.EqualTo(Color.green));
            _layer.Hide();
            Assert.That(_layer.View.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(_layer.ShowFilled(_map, cells, preview, _material, Color.red, Color.green), Is.True);
            Assert.That(_layer.View.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            Assert.That(_layer.View.GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void InvalidGeometryHidesThePreviousDisplayAndCanRecover()
        {
            HexCoordinate[] valid = { new HexCoordinate(0, 0) };
            HexCoordinate[] invalid = { new HexCoordinate(-1, 0) };
            Assert.That(_layer.ShowFilled(_map, valid, null, _material, Color.red, Color.blue), Is.True);
            Assert.That(_layer.ShowFilled(_map, invalid, null, _material, Color.red, Color.blue), Is.False);
            Assert.That(_layer.View.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(_layer.ShowFilled(_map, valid, null, _material, Color.red, Color.blue), Is.True);
            Assert.That(_layer.View.GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void MapReplacementReleasesTheOldMeshAndUsesTheNewLayer()
        {
            HexCoordinate[] cells = { new HexCoordinate(0, 0) };
            Assert.That(_layer.ShowFilled(_map, cells, null, _material, Color.red, Color.blue), Is.True);
            BattleRangeMeshView previousView = _layer.View;
            Mesh previousMesh = previousView.GetComponent<MeshFilter>().sharedMesh;
            HexMapView replacement = new GameObject("Replacement Map").AddComponent<HexMapView>();

            try
            {
                replacement.gameObject.layer = 2;
                Assert.That(_layer.ShowFilled(replacement, cells, null, _material, Color.red, Color.blue), Is.True);
                Assert.That(previousView == null, Is.True);
                Assert.That(previousMesh == null, Is.True);
                Assert.That(_layer.View.transform.parent, Is.EqualTo(replacement.transform));
                Assert.That(_layer.View.gameObject.layer, Is.EqualTo(2));
                replacement.gameObject.layer = 0;
                Assert.That(_layer.ShowFilled(replacement, cells, null, _material, Color.red, Color.blue), Is.True);
                Assert.That(_layer.View.gameObject.layer, Is.EqualTo(0));
            }
            finally
            {
                _layer.Release();
                Object.DestroyImmediate(replacement.gameObject);
            }
        }
    }
}
