using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleDamageFanLayoutTests
    {
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(7)]
        public void ItemsShareAnOriginAndSeparateAfterSpreading(int count)
        {
            Vector2[] sizes = new Vector2[count];
            Vector2[] variations = new Vector2[count];
            Vector2[] destinations = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                sizes[i] = new Vector2(i == count - 1 ? 86f : 32f, 30f);
                variations[i] = new Vector2(i % 2 == 0 ? -2f : 2f, i % 2 == 0 ? 0.96f : 1.04f);
            }

            Rect viewport = new Rect(-400f, -300f, 800f, 600f);
            Assert.That(BattleDamageFanLayout.TryArrange(sizes, variations, count, viewport,
                Vector2.zero, destinations, 36f, 18f, out Vector2 origin, out Rect reservation, out float rise), Is.True);
            AssertContains(viewport, reservation);

            for (int i = 0; i < count; i++)
            {
                Assert.That(BattleDamageFanLayout.Evaluate(origin, destinations[i], 0f, rise, 0.2f, 0.8f), Is.EqualTo(origin));
                Assert.That(destinations[i].y, Is.GreaterThan(0f));
                Assert.That(Mathf.Abs(Mathf.Atan2(destinations[i].x, destinations[i].y) * Mathf.Rad2Deg),
                    Is.LessThanOrEqualTo(50.001f));

                for (int sample = 0; sample <= 8; sample++)
                {
                    float elapsed = sample * 0.1f;
                    Vector2 position = BattleDamageFanLayout.Evaluate(origin, destinations[i], elapsed, rise, 0.2f, 0.8f);
                    Rect rect = new Rect(position - sizes[i] * 0.5f, sizes[i]);
                    AssertContains(reservation, rect);

                    if (elapsed < 0.2f)
                    {
                        continue;
                    }

                    for (int j = 0; j < i; j++)
                    {
                        Vector2 other = BattleDamageFanLayout.Evaluate(origin, destinations[j], elapsed, rise, 0.2f, 0.8f);
                        Assert.That(rect.Overlaps(new Rect(other - sizes[j] * 0.5f, sizes[j])), Is.False);
                    }
                }
            }
        }

        [TestCase(-1000f, -1000f)]
        [TestCase(1000f, 1000f)]
        public void ScreenEdgesClampTheWholeMotionReservation(float x, float y)
        {
            Vector2[] sizes = { new Vector2(34f, 28f), new Vector2(34f, 28f), new Vector2(34f, 28f), new Vector2(34f, 28f) };
            Vector2[] variations = { Vector2.up, Vector2.up, Vector2.up, Vector2.up };
            Vector2[] destinations = new Vector2[4];
            Rect viewport = new Rect(-80f, -210f, 400f, 300f);
            Assert.That(BattleDamageFanLayout.TryArrange(sizes, variations, 4, viewport, new Vector2(x, y),
                destinations, 36f, 18f, out _, out Rect reservation, out _), Is.True);
            AssertContains(viewport, reservation);
        }

        [Test]
        public void SingleItemCanFitWithoutMotionInAViewportExactlyItsPaddedSize()
        {
            Vector2[] sizes = { new Vector2(12f, 12f) };
            Vector2[] variations = { Vector2.up };
            Vector2[] destinations = new Vector2[1];
            Rect viewport = new Rect(0f, 0f, 20f, 20f);
            Assert.That(BattleDamageFanLayout.TryArrange(sizes, variations, 1, viewport, Vector2.zero,
                destinations, 36f, 18f, out Vector2 origin, out Rect reservation, out float rise), Is.True);
            Assert.That(rise, Is.Zero);
            Assert.That(destinations[0], Is.EqualTo(Vector2.zero));
            Assert.That(BattleDamageFanLayout.Evaluate(origin, destinations[0], 0.7f, rise, 0.2f, 0.8f), Is.EqualTo(origin));
            AssertContains(viewport, reservation);
        }

        [Test]
        public void OversizedTextRejectsLayoutInsteadOfOverlappingOrLooping()
        {
            Vector2[] sizes = { new Vector2(120f, 40f) };
            Vector2[] variations = { Vector2.up };
            Assert.That(BattleDamageFanLayout.TryArrange(sizes, variations, 1, new Rect(0f, 0f, 100f, 100f),
                Vector2.zero, new Vector2[1], 36f, 18f, out _, out _, out _), Is.False);
        }

        [Test]
        public void PositionDependsOnElapsedTimeRatherThanPreviousFrames()
        {
            Vector2 origin = new Vector2(20f, 30f);
            Vector2 destination = new Vector2(-30f, 50f);
            Vector2 direct = BattleDamageFanLayout.Evaluate(origin, destination, 0.45f, 18f, 0.2f, 0.8f);

            for (int i = 0; i < 27; i++)
            {
                BattleDamageFanLayout.Evaluate(origin, destination, i / 60f, 18f, 0.2f, 0.8f);
            }

            Assert.That(BattleDamageFanLayout.Evaluate(origin, destination, 0.45f, 18f, 0.2f, 0.8f), Is.EqualTo(direct));
        }

        [Test]
        public void CustomTimingsControlSpreadAndSubsequentRise()
        {
            Vector2 destination = new Vector2(20f, 40f);
            Assert.That(BattleDamageFanLayout.Evaluate(Vector2.zero, destination, 0.25f, 8f, 0.5f, 1.5f),
                Is.EqualTo(destination * 0.75f));
            Assert.That(BattleDamageFanLayout.Evaluate(Vector2.zero, destination, 0.5f, 8f, 0.5f, 1.5f),
                Is.EqualTo(destination));
            Assert.That(BattleDamageFanLayout.Evaluate(Vector2.zero, destination, 1f, 8f, 0.5f, 1.5f),
                Is.EqualTo(destination + Vector2.up * 4f));
        }

        [Test]
        public void CustomDistancesAreIncludedInTheReservedMotionArea()
        {
            Vector2[] sizes = { new Vector2(20f, 20f) };
            Vector2[] variations = { Vector2.up };
            Vector2[] destinations = new Vector2[1];
            Assert.That(BattleDamageFanLayout.TryArrange(sizes, variations, 1,
                new Rect(-500f, -500f, 1000f, 1000f), Vector2.zero, destinations, 75f, 40f,
                out Vector2 origin, out Rect reservation, out float rise), Is.True);
            Assert.That(destinations[0], Is.EqualTo(Vector2.up * 75f));
            Assert.That(rise, Is.EqualTo(40f));
            Vector2 final = BattleDamageFanLayout.Evaluate(origin, destinations[0], 1.2f, rise, 0.5f, 1.2f);
            AssertContains(reservation, new Rect(final - sizes[0] * 0.5f, sizes[0]));
        }

        static void AssertContains(Rect outer, Rect inner)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 0.001f));
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 0.001f));
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 0.001f));
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 0.001f));
        }
    }
}
