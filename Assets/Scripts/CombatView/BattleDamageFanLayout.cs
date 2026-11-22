using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    static class BattleDamageFanLayout
    {
        const float _padding = 4f;

        internal static bool TryArrange(Vector2[] sizes, Vector2[] variations, int count, Rect viewport,
            Vector2 desiredOrigin, Vector2[] destinations, float spreadDistance, float riseDistance,
            out Vector2 origin, out Rect reservation, out float rise)
        {
            origin = default;
            reservation = default;
            rise = riseDistance;

            if (count < 1 || count > sizes.Length || count > variations.Length || count > destinations.Length
                || viewport.width <= 0f || viewport.height <= 0f)
            {
                return false;
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                float radius = spreadDistance + attempt * 24f;
                bool separated = true;
                Vector2 minimum = Vector2.zero;
                Vector2 maximum = Vector2.zero;

                for (int i = 0; i < count; i++)
                {
                    float angle = Mathf.Clamp((count == 1 ? 0f : Mathf.Lerp(-50f, 50f, (float)i / (count - 1)))
                        + variations[i].x, -50f, 50f);
                    float distance = radius * variations[i].y;
                    destinations[i] = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad),
                        Mathf.Cos(angle * Mathf.Deg2Rad)) * distance;
                    Vector2 half = sizes[i] * 0.5f + Vector2.one * _padding;
                    minimum = Vector2.Min(minimum, Vector2.Min(-half, destinations[i] - half));
                    maximum = Vector2.Max(maximum,
                        Vector2.Max(half, destinations[i] + half + Vector2.up * riseDistance));
                    Rect item = new Rect(destinations[i] - half, half * 2f);

                    for (int j = 0; j < i; j++)
                    {
                        Vector2 otherHalf = sizes[j] * 0.5f + Vector2.one * _padding;

                        if (item.Overlaps(new Rect(destinations[j] - otherHalf, otherHalf * 2f)))
                        {
                            separated = false;
                        }
                    }
                }

                Vector2 extent = maximum - minimum;

                if (!separated || extent.x > viewport.width || extent.y > viewport.height)
                {
                    continue;
                }

                origin = new Vector2(Mathf.Clamp(desiredOrigin.x, viewport.xMin - minimum.x, viewport.xMax - maximum.x),
                    Mathf.Clamp(desiredOrigin.y, viewport.yMin - minimum.y, viewport.yMax - maximum.y));
                reservation = new Rect(origin + minimum, extent);
                return true;
            }

            if (count == 1 && sizes[0].x + _padding * 2f <= viewport.width
                && sizes[0].y + _padding * 2f <= viewport.height)
            {
                Vector2 half = sizes[0] * 0.5f + Vector2.one * _padding;
                float room = Mathf.Max(0f, viewport.height - half.y * 2f);
                rise = Mathf.Min(riseDistance, room * 0.5f);
                destinations[0] = Vector2.up * Mathf.Min(spreadDistance, room * 0.5f);
                Vector2 extent = half * 2f + Vector2.up * (destinations[0].y + rise);
                origin = new Vector2(Mathf.Clamp(desiredOrigin.x, viewport.xMin + half.x, viewport.xMax - half.x),
                    Mathf.Clamp(desiredOrigin.y, viewport.yMin + half.y, viewport.yMax - extent.y + half.y));
                reservation = new Rect(origin - half, extent);
                return true;
            }

            return false;
        }

        internal static Vector2 Evaluate(Vector2 origin, Vector2 destination, float elapsed, float rise,
            float spreadDuration, float displayDuration)
        {
            float progress = Mathf.Clamp01(elapsed / spreadDuration);
            float eased = 1f - (1f - progress) * (1f - progress);
            float drift = displayDuration > spreadDuration
                ? Mathf.Clamp01((elapsed - spreadDuration) / (displayDuration - spreadDuration)) : 0f;
            return origin + Vector2.LerpUnclamped(Vector2.zero, destination, eased) + Vector2.up * (rise * drift);
        }
    }
}
