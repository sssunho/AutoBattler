using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleProjectilePathTests
    {
        readonly BattleProjectilePath _path = new BattleProjectilePath();
        readonly List<HexCoordinate> _cells = new List<HexCoordinate>();
        HexMapView _map;

        [SetUp]
        public void SetUp()
        {
            _map = new GameObject("Projectile Path Test Map").AddComponent<HexMapView>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_map.gameObject);
        }

        [Test]
        public void LongSegmentIncludesEveryCrossedCellInTravelOrder()
        {
            _path.Copy(_map, World(1, 2), World(6, 2), _cells);
            Assert.That(_cells, Is.EqualTo(new[]
            {
                new HexCoordinate(1, 2), new HexCoordinate(2, 2), new HexCoordinate(3, 2),
                new HexCoordinate(4, 2), new HexCoordinate(5, 2), new HexCoordinate(6, 2)
            }));
            _path.Copy(_map, World(6, 2), World(1, 2), _cells);
            Assert.That(_cells[0], Is.EqualTo(new HexCoordinate(6, 2)));
            Assert.That(_cells[_cells.Count - 1], Is.EqualTo(new HexCoordinate(1, 2)));
            Assert.That(_cells.Count, Is.EqualTo(6));
        }

        [Test]
        public void SharedEdgeIncludesBothTouchingCells()
        {
            BattleRangeOutline.GetLocalEdge(new BattleHexEdge(new HexCoordinate(3, 3), HexDirection.East),
                _map.HexSize, out Vector3 a, out Vector3 b);
            Vector3 start = Vector3.Lerp(a, b, 0.2f);
            Vector3 end = Vector3.Lerp(a, b, 0.8f);
            _path.Copy(_map, _map.transform.TransformPoint(start), _map.transform.TransformPoint(end), _cells);
            Assert.That(_cells, Has.Member(new HexCoordinate(3, 3)));
            Assert.That(_cells, Has.Member(new HexCoordinate(3, 3).GetNeighbor(HexDirection.East)));
            Assert.That(_cells.Count, Is.EqualTo(2));
        }

        [Test]
        public void HeightAndMapTransformDoNotChangeGroundProjection()
        {
            _map.transform.SetPositionAndRotation(new Vector3(7f, 2f, -3f), Quaternion.Euler(10f, 45f, 0f));
            _map.transform.localScale = Vector3.one * 1.4f;
            _path.Copy(_map, World(1, 2) + _map.transform.up * 3f,
                World(4, 2) + _map.transform.up, _cells);
            Assert.That(_cells.Count, Is.EqualTo(4));
            Assert.That(_cells[0], Is.EqualTo(new HexCoordinate(1, 2)));
            Assert.That(_cells[3], Is.EqualTo(new HexCoordinate(4, 2)));
        }

        [Test]
        public void StationaryCenterProducesOneCellAndInvalidSegmentClearsOutput()
        {
            _path.Copy(_map, World(2, 2), World(2, 2), _cells);
            Assert.That(_cells, Is.EqualTo(new[] { new HexCoordinate(2, 2) }));
            _path.Copy(_map, new Vector3(float.NaN, 0f, 0f), World(2, 2), _cells);
            Assert.That(_cells, Is.Empty);
        }

        Vector3 World(int q, int r)
        {
            return _map.AxialToWorld(new HexCoordinate(q, r));
        }
    }
}
