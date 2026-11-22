using System;

namespace RaggedBanner.Animations
{
    public static class AnimationFrameUtility
    {
        const double _frameTolerance = 0.0001;

        public static bool TryGetLastFrame(float length, float frameRate, out int lastFrame)
        {
            lastFrame = 0;

            if (length <= 0f || float.IsNaN(length) || float.IsInfinity(length)
                || frameRate <= 0f || float.IsNaN(frameRate) || float.IsInfinity(frameRate))
            {
                return false;
            }

            double frame = Math.Floor((double)length * frameRate + _frameTolerance);

            if (frame > int.MaxValue)
            {
                return false;
            }

            lastFrame = (int)frame;
            return true;
        }

        public static bool TryGetTime(int frame, float length, float frameRate, out double time)
        {
            time = 0d;

            if (!TryGetLastFrame(length, frameRate, out int lastFrame) || frame < 0 || frame > lastFrame)
            {
                return false;
            }

            time = Math.Min(frame / (double)frameRate, length);
            return true;
        }

        public static bool TryGetFrame(double time, float length, float frameRate, out int frame)
        {
            frame = 0;

            if (!TryGetLastFrame(length, frameRate, out int lastFrame)
                || double.IsNaN(time) || double.IsInfinity(time) || time < 0d || time > length)
            {
                return false;
            }

            double sampledFrame = Math.Floor(time * frameRate + _frameTolerance);
            frame = (int)Math.Min(sampledFrame, lastFrame);

            if (frame > 0 && Math.Min(frame / (double)frameRate, length) > time)
            {
                frame--;
            }

            return true;
        }
    }
}
