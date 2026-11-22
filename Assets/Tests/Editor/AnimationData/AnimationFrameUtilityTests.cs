using NUnit.Framework;

namespace RaggedBanner.Animations.Tests
{
    public sealed class AnimationFrameUtilityTests
    {
        [TestCase(1f, 30f, 30)]
        [TestCase(1f, 60f, 60)]
        [TestCase(1f, 29.97f, 29)]
        [TestCase(2f, 29.97f, 59)]
        [TestCase(0.01f, 30f, 0)]
        [TestCase(1f / 30f, 30f, 1)]
        [TestCase(0.999999f, 30f, 30)]
        [TestCase(0.99999f, 30f, 29)]
        public void LastFrameIncludesTheEndpointWithinTheApprovedTolerance(float length, float frameRate, int expected)
        {
            Assert.That(AnimationFrameUtility.TryGetLastFrame(length, frameRate, out int lastFrame), Is.True);
            Assert.That(lastFrame, Is.EqualTo(expected));
        }

        [TestCase(0f, 30f)]
        [TestCase(-1f, 30f)]
        [TestCase(float.NaN, 30f)]
        [TestCase(float.PositiveInfinity, 30f)]
        [TestCase(float.NegativeInfinity, 30f)]
        [TestCase(1f, 0f)]
        [TestCase(1f, -1f)]
        [TestCase(1f, float.NaN)]
        [TestCase(1f, float.PositiveInfinity)]
        [TestCase(1f, float.NegativeInfinity)]
        [TestCase(float.MaxValue, float.MaxValue)]
        [TestCase(1f, 2147483648f)]
        public void InvalidOrUnrepresentableTimelinesAreRejected(float length, float frameRate)
        {
            Assert.That(AnimationFrameUtility.TryGetLastFrame(length, frameRate, out int lastFrame), Is.False);
            Assert.That(lastFrame, Is.Zero);
            Assert.That(AnimationFrameUtility.TryGetTime(0, length, frameRate, out double time), Is.False);
            Assert.That(time, Is.Zero);
            Assert.That(AnimationFrameUtility.TryGetFrame(0d, length, frameRate, out int frame), Is.False);
            Assert.That(frame, Is.Zero);
        }

        [Test]
        public void LargeRepresentableFrameCountsDoNotOverflow()
        {
            Assert.That(AnimationFrameUtility.TryGetLastFrame(1f, 2147483520f, out int lastFrame), Is.True);
            Assert.That(lastFrame, Is.EqualTo(2147483520));
            Assert.That(AnimationFrameUtility.TryGetTime(lastFrame, 1f, 2147483520f, out double time), Is.True);
            Assert.That(time, Is.EqualTo(1d));
        }

        [TestCase(-1)]
        [TestCase(31)]
        [TestCase(int.MaxValue)]
        public void FramesOutsideTheTimelineAreNotSilentlyClamped(int frame)
        {
            Assert.That(AnimationFrameUtility.TryGetTime(frame, 1f, 30f, out double time), Is.False);
            Assert.That(time, Is.Zero);
        }

        [Test]
        public void FirstFrameStartsAtZeroAndEndpointNeverSamplesPastLength()
        {
            Assert.That(AnimationFrameUtility.TryGetTime(0, 0.999999f, 30f, out double first), Is.True);
            Assert.That(first, Is.Zero);
            Assert.That(AnimationFrameUtility.TryGetTime(30, 0.999999f, 30f, out double last), Is.True);
            Assert.That(last, Is.EqualTo((double)0.999999f));
            Assert.That(AnimationFrameUtility.TryGetFrame(last, 0.999999f, 30f, out int restored), Is.True);
            Assert.That(restored, Is.EqualTo(30));
        }

        [Test]
        public void ASubframeClipHasOneSelectableFrameWithoutDivisionByZero()
        {
            Assert.That(AnimationFrameUtility.TryGetTime(0, 0.01f, 30f, out double time), Is.True);
            Assert.That(time, Is.Zero);
            Assert.That(AnimationFrameUtility.TryGetTime(1, 0.01f, 30f, out _), Is.False);
            Assert.That(AnimationFrameUtility.TryGetFrame(0.01f, 0.01f, 30f, out int frame), Is.True);
            Assert.That(frame, Is.Zero);
        }

        [TestCase(0d, 0)]
        [TestCase(0.000001d, 0)]
        [TestCase(1d / 30d, 1)]
        [TestCase(0.499999d, 14)]
        [TestCase(0.5d, 15)]
        [TestCase(0.51d, 15)]
        [TestCase(0.999999d, 29)]
        [TestCase(1d, 30)]
        public void TimeConversionUsesThePrecedingFrame(double time, int expected)
        {
            Assert.That(AnimationFrameUtility.TryGetFrame(time, 1f, 30f, out int frame), Is.True);
            Assert.That(frame, Is.EqualTo(expected));
        }

        [TestCase(-0.001d)]
        [TestCase(1.001d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void InvalidTimesAreRejected(double time)
        {
            Assert.That(AnimationFrameUtility.TryGetFrame(time, 1f, 30f, out int frame), Is.False);
            Assert.That(frame, Is.Zero);
        }

        [TestCase(30f)]
        [TestCase(60f)]
        [TestCase(29.97f)]
        public void EverySelectableFrameSurvivesATimeRoundTrip(float frameRate)
        {
            Assert.That(AnimationFrameUtility.TryGetLastFrame(2f, frameRate, out int lastFrame), Is.True);

            for (int frame = 0; frame <= lastFrame; frame++)
            {
                Assert.That(AnimationFrameUtility.TryGetTime(frame, 2f, frameRate, out double time), Is.True);
                Assert.That(AnimationFrameUtility.TryGetFrame(time, 2f, frameRate, out int restored), Is.True);
                Assert.That(restored, Is.EqualTo(frame));
            }
        }
    }
}
