using System;
using NUnit.Framework;
using UnityEngine;
using AutoBattler.HexGrid.Unity;

namespace AutoBattler.HexGrid.Tests
{
    public sealed class HexLayoutTests
    {
        [Test]
        public void AxialToLocal_ReturnsPointyTopCenter()
        {
            Vector2 position = HexLayout.AxialToLocal(new HexCoordinate(1, -1), 2f);

            Assert.That(position.x, Is.EqualTo(Mathf.Sqrt(3f)).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(3f).Within(0.0001f));
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        [TestCase(0, -1)]
        [TestCase(-1, 0)]
        [TestCase(-1, 1)]
        [TestCase(0, 1)]
        [TestCase(3, -2)]
        public void LocalToAxial_RestoresCoordinateFromCenter(int q, int r)
        {
            HexCoordinate coordinate = new HexCoordinate(q, r);
            Vector2 position = HexLayout.AxialToLocal(coordinate, 1.25f);

            HexCoordinate result = HexLayout.LocalToAxial(position, 1.25f);

            Assert.That(result, Is.EqualTo(coordinate));
        }

        [Test]
        public void LocalToAxial_RoundsPositionToNearestCell()
        {
            Vector2 center = HexLayout.AxialToLocal(new HexCoordinate(2, 1), 1f);
            Vector2 position = center + new Vector2(0.1f, -0.1f);

            Assert.That(HexLayout.LocalToAxial(position, 1f), Is.EqualTo(new HexCoordinate(2, 1)));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Conversion_ThrowsForNonPositiveHexSize(float hexSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HexLayout.AxialToLocal(new HexCoordinate(0, 0), hexSize));
            Assert.Throws<ArgumentOutOfRangeException>(() => HexLayout.LocalToAxial(Vector2.zero, hexSize));
        }

        [Test]
        public void HexMapView_AppliesBoardTransformAndIgnoresLocalY()
        {
            GameObject gameObject = new GameObject("HexMapView Test");

            try
            {
                gameObject.transform.position = new Vector3(4f, 2f, -3f);
                gameObject.transform.rotation = Quaternion.Euler(0f, 37f, 0f);
                gameObject.transform.localScale = new Vector3(2f, 1f, 1.5f);
                HexMapView view = gameObject.AddComponent<HexMapView>();
                HexCoordinate coordinate = new HexCoordinate(2, -1);
                Vector3 worldPosition = view.AxialToWorld(coordinate);
                worldPosition += gameObject.transform.up * 5f;

                Assert.That(view.WorldToAxial(worldPosition), Is.EqualTo(coordinate));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
