using UnityEngine;

namespace RaggedBanner.WorldMap.Unity
{
    internal sealed class WorldMapRoadPath
    {
        const int _maxSegments = 256;
        const float _sampleSpacing = 12f;
        const float _minimumDistance = 0.001f;

        readonly Vector2[] _points = new Vector2[_maxSegments + 1];
        readonly float[] _distances = new float[_maxSegments + 1];
        int _count;
        float _length;

        internal int Count => _count;
        internal float Length => _length;

        internal void Rebuild(WorldMapRoadDefinition road, Vector2 locationA, Vector2 locationB, Vector2 mapSize)
        {
            _count = 0;
            _length = 0f;

            Vector2 start = locationA + road.StartOffset;
            Vector2 end = locationB + road.EndOffset;
            Vector2 controlA = Vector2.LerpUnclamped(start, end, 1f / 3f) + road.ControlOffsetA;
            Vector2 controlB = Vector2.LerpUnclamped(start, end, 2f / 3f) + road.ControlOffsetB;
            Vector2 center = new Vector2(0.5f, 0.5f);
            start = Vector2.Scale(start - center, mapSize);
            end = Vector2.Scale(end - center, mapSize);
            controlA = Vector2.Scale(controlA - center, mapSize);
            controlB = Vector2.Scale(controlB - center, mapSize);

            if (!IsFinite(start) || !IsFinite(end) || !IsFinite(controlA) || !IsFinite(controlB))
            {
                return;
            }

            float estimate = Vector2.Distance(start, controlA)
                + Vector2.Distance(controlA, controlB) + Vector2.Distance(controlB, end);

            if (float.IsNaN(estimate) || float.IsInfinity(estimate))
            {
                return;
            }

            int segments = Mathf.CeilToInt(Mathf.Clamp(estimate / _sampleSpacing, 8f, _maxSegments));
            _points[0] = start;
            _distances[0] = 0f;
            _count = 1;

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float remaining = 1f - t;
                Vector2 point = remaining * remaining * remaining * start
                    + 3f * remaining * remaining * t * controlA
                    + 3f * remaining * t * t * controlB
                    + t * t * t * end;
                float distance = Vector2.Distance(_points[_count - 1], point);

                if (distance <= _minimumDistance)
                {
                    continue;
                }

                _length += distance;
                _points[_count] = point;
                _distances[_count] = _length;
                _count++;
            }
        }

        internal Vector2 GetPoint(int index)
        {
            return _points[index];
        }

        internal float GetDistance(int index)
        {
            return _distances[index];
        }

        internal Vector2 Evaluate(float normalizedDistance)
        {
            if (_count == 0)
            {
                return Vector2.zero;
            }

            if (_count == 1 || normalizedDistance <= 0f || float.IsNaN(normalizedDistance))
            {
                return _points[0];
            }

            if (normalizedDistance >= 1f)
            {
                return _points[_count - 1];
            }

            float distance = normalizedDistance * _length;
            int lower = 0;
            int upper = _count - 1;

            while (upper - lower > 1)
            {
                int middle = (lower + upper) / 2;

                if (_distances[middle] < distance)
                {
                    lower = middle;
                }
                else
                {
                    upper = middle;
                }
            }

            float span = _distances[upper] - _distances[lower];
            float progress = span > _minimumDistance ? (distance - _distances[lower]) / span : 0f;
            return Vector2.LerpUnclamped(_points[lower], _points[upper], progress);
        }

        static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }
    }
}
