using System;
using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRangeOutlineTests
    {
        readonly BattleRangeOutline _outline = new BattleRangeOutline();
        readonly List<BattleHexEdge> _edges = new List<BattleHexEdge>();

        [Test]
        public void AdjacentDestinationIncludesTheOriginAndRemovesTheirSharedEdge()
        {
            HexCoordinate origin = new HexCoordinate(2, 2);
            HexCoordinate destination = origin.GetNeighbor(HexDirection.East);
            _outline.Build(8, 8, origin, new[] { destination }, _edges);

            Assert.That(_edges.Count, Is.EqualTo(10));
            Assert.That(ContainsEdge(origin, HexDirection.East), Is.False);
            Assert.That(ContainsEdge(destination, HexDirection.West), Is.False);
            Assert.That(ContainsEdge(origin, HexDirection.West), Is.True);
            Assert.That(ContainsEdge(destination, HexDirection.East), Is.True);
        }

        [Test]
        public void AllSixDestinationsAroundTheOriginProduceOnlyTheOuterPerimeter()
        {
            HexCoordinate origin = new HexCoordinate(3, 3);
            HexCoordinate[] ring = CreateRing(origin);
            _outline.Build(8, 8, origin, ring, _edges);

            Assert.That(_edges.Count, Is.EqualTo(18));

            for (int i = 0; i < _edges.Count; i++)
            {
                Assert.That(_edges[i].Coordinate, Is.Not.EqualTo(origin));
                Assert.That(_edges[i].Coordinate.GetNeighbor(_edges[i].Direction), Is.Not.EqualTo(origin));
            }
        }

        [Test]
        public void EnclosedHoleHasNoOutlineEvenWhenTheOriginIsOnItsRing()
        {
            HexCoordinate hole = new HexCoordinate(3, 3);
            HexCoordinate[] ring = CreateRing(hole);
            _outline.Build(8, 8, ring[0], ring, _edges);

            Assert.That(_edges.Count, Is.EqualTo(18));

            for (int i = 0; i < _edges.Count; i++)
            {
                Assert.That(_edges[i].Coordinate.GetNeighbor(_edges[i].Direction), Is.Not.EqualTo(hole));
            }
        }

        [Test]
        public void OpeningTheRingMakesTheInnerEdgesPartOfTheExterior()
        {
            HexCoordinate center = new HexCoordinate(3, 3);
            HexCoordinate[] ring = CreateRing(center);
            List<HexCoordinate> openRing = new List<HexCoordinate>();

            for (int i = 1; i < ring.Length; i++)
            {
                openRing.Add(ring[i]);
            }

            _outline.Build(8, 8, ring[1], openRing, _edges);

            Assert.That(_edges.Count, Is.EqualTo(22));
            Assert.That(ContainsEdge(ring[1], HexDirection.SouthWest), Is.True);
        }

        [Test]
        public void ConcaveRegionKeepsItsExposedIndentedEdges()
        {
            HexCoordinate origin = new HexCoordinate(2, 2);
            HexCoordinate east = origin.GetNeighbor(HexDirection.East);
            HexCoordinate end = east.GetNeighbor(HexDirection.SouthEast);
            _outline.Build(8, 8, origin, new[] { east, end }, _edges);

            Assert.That(_edges.Count, Is.EqualTo(14));
            Assert.That(ContainsEdge(origin, HexDirection.SouthEast), Is.True);
            Assert.That(ContainsEdge(end, HexDirection.West), Is.True);
            Assert.That(ContainsEdge(east, HexDirection.SouthEast), Is.False);
        }

        [Test]
        public void RegionTouchingEveryMapBoundaryStillHasACompleteExterior()
        {
            HexCoordinate origin = new HexCoordinate(0, 0);
            _outline.Build(2, 2, origin, new[]
            {
                new HexCoordinate(1, 0),
                new HexCoordinate(0, 1),
                new HexCoordinate(1, 1)
            }, _edges);

            Assert.That(_edges.Count, Is.EqualTo(14));
            Assert.That(ContainsEdge(origin, HexDirection.West), Is.True);
            Assert.That(ContainsEdge(origin, HexDirection.NorthWest), Is.True);
            Assert.That(ContainsEdge(new HexCoordinate(1, 1), HexDirection.East), Is.True);
            Assert.That(ContainsEdge(new HexCoordinate(1, 1), HexDirection.SouthEast), Is.True);
        }

        [Test]
        public void DuplicateOriginAndInvalidDestinationsDoNotChangeTheOutline()
        {
            HexCoordinate origin = new HexCoordinate(2, 2);
            HexCoordinate destination = origin.GetNeighbor(HexDirection.East);
            _outline.Build(8, 8, origin, new[]
            {
                origin, destination, destination, new HexCoordinate(-1, 2), new HexCoordinate(8, 2)
            }, _edges);

            Assert.That(_edges.Count, Is.EqualTo(10));
        }

        [Test]
        public void NoValidMovementDestinationClearsThePreviousOutline()
        {
            HexCoordinate origin = new HexCoordinate(2, 2);
            _outline.Build(8, 8, origin, new[] { origin.GetNeighbor(HexDirection.East) }, _edges);
            Assert.That(_edges, Is.Not.Empty);

            _outline.Build(8, 8, origin, new[] { origin, new HexCoordinate(-1, 0) }, _edges);
            Assert.That(_edges, Is.Empty);
            _outline.Build(8, 8, origin, Array.Empty<HexCoordinate>(), _edges);
            Assert.That(_edges, Is.Empty);
            _outline.Build(8, 8, origin, null, _edges);
            Assert.That(_edges, Is.Empty);
        }

        [TestCase(0, 8, 0, 0)]
        [TestCase(8, 0, 0, 0)]
        [TestCase(-1, 8, 0, 0)]
        [TestCase(8, 8, -1, 0)]
        [TestCase(8, 8, 8, 0)]
        [TestCase(8, 8, 0, 8)]
        public void InvalidMapOrOriginClearsTheOutput(int width, int height, int q, int r)
        {
            _edges.Add(new BattleHexEdge(new HexCoordinate(1, 1), HexDirection.East));
            _outline.Build(width, height, new HexCoordinate(q, r), new[] { new HexCoordinate(1, 0) }, _edges);
            Assert.That(_edges, Is.Empty);
        }

        [Test]
        public void ReuseAfterAnotherMapOrNullOutputDoesNotRetainThePreviousRegion()
        {
            HexCoordinate first = new HexCoordinate(1, 1);
            _outline.Build(8, 8, first, new[] { first.GetNeighbor(HexDirection.East) }, _edges);
            _outline.Build(8, 8, first, new[] { first.GetNeighbor(HexDirection.East) }, null);
            HexCoordinate second = new HexCoordinate(12, 12);
            _outline.Build(16, 16, second, new[] { second.GetNeighbor(HexDirection.West) }, _edges);

            Assert.That(_edges.Count, Is.EqualTo(10));

            for (int i = 0; i < _edges.Count; i++)
            {
                Assert.That(_edges[i].Coordinate.Q, Is.InRange(11, 12));
                Assert.That(_edges[i].Coordinate.R, Is.EqualTo(12));
            }
        }

        [TestCase(HexDirection.East)]
        [TestCase(HexDirection.NorthEast)]
        [TestCase(HexDirection.NorthWest)]
        [TestCase(HexDirection.West)]
        [TestCase(HexDirection.SouthWest)]
        [TestCase(HexDirection.SouthEast)]
        public void EachDirectionProducesTheSharedEdgeWithTheOppositeNeighbor(HexDirection direction)
        {
            const float hexSize = 2.25f;
            HexCoordinate origin = new HexCoordinate(3, 3);
            HexCoordinate neighbor = origin.GetNeighbor(direction);
            HexDirection opposite = (HexDirection)(((int)direction + 3) % 6);
            BattleRangeOutline.GetLocalEdge(new BattleHexEdge(origin, direction), hexSize,
                out Vector3 start, out Vector3 end);
            BattleRangeOutline.GetLocalEdge(new BattleHexEdge(neighbor, opposite), hexSize,
                out Vector3 otherStart, out Vector3 otherEnd);
            Vector2 originPosition = HexLayout.AxialToLocal(origin, hexSize);
            Vector2 neighborPosition = HexLayout.AxialToLocal(neighbor, hexSize);
            Vector3 midpoint = new Vector3((originPosition.x + neighborPosition.x) * 0.5f,
                0f, (originPosition.y + neighborPosition.y) * 0.5f);

            Assert.That(Vector3.Distance(start, otherEnd), Is.LessThan(0.00001f));
            Assert.That(Vector3.Distance(end, otherStart), Is.LessThan(0.00001f));
            Assert.That(Vector3.Distance((start + end) * 0.5f, midpoint), Is.LessThan(0.00001f));
            Assert.That(Vector3.Distance(start, end), Is.EqualTo(hexSize).Within(0.00001f));
        }

        [Test]
        public void InvalidDirectionAndSizeAreRejectedBeforeProducingGeometry()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleRangeOutline.GetLocalEdge(
                new BattleHexEdge(default, (HexDirection)6), 1f, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleRangeOutline.GetLocalEdge(
                new BattleHexEdge(default, HexDirection.East), 0f, out _, out _));
        }

        bool ContainsEdge(HexCoordinate coordinate, HexDirection direction)
        {
            for (int i = 0; i < _edges.Count; i++)
            {
                if (_edges[i].Coordinate == coordinate && _edges[i].Direction == direction)
                {
                    return true;
                }
            }

            return false;
        }

        static HexCoordinate[] CreateRing(HexCoordinate center)
        {
            HexCoordinate[] ring = new HexCoordinate[6];

            for (int i = 0; i < ring.Length; i++)
            {
                ring[i] = center.GetNeighbor((HexDirection)i);
            }

            return ring;
        }
    }
}
