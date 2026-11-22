using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public static class BattleOverwatchArea
    {
        public static bool Contains(HexCoordinate origin, HexDirection direction, int range, HexCoordinate target, int minRange = 1)
        {
            if ((uint)direction >= 6 || range <= 0 || origin == target)
            {
                return false;
            }

            long q = (long)target.Q - origin.Q;
            long r = (long)target.R - origin.R;
            long distance = Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(q + r)));

            if (distance < minRange || distance > range)
            {
                return false;
            }

            HexCoordinate left = default(HexCoordinate).GetNeighbor((HexDirection)(((int)direction + 1) % 6));
            HexCoordinate right = default(HexCoordinate).GetNeighbor((HexDirection)(((int)direction + 5) % 6));
            return left.Q * r - left.R * q >= 0 && q * right.R - r * right.Q >= 0;
        }

        public static void CopyCells(int width, int height, HexCoordinate origin, HexDirection direction,
            int range, List<HexCoordinate> destination, int minRange = 1)
        {
            destination?.Clear();

            if (destination == null || width <= 0 || height <= 0 || (uint)direction >= 6 || range <= 0
                || origin.Q < 0 || origin.R < 0 || origin.Q >= width || origin.R >= height)
            {
                return;
            }

            int minQ = (int)Math.Max(0L, (long)origin.Q - range);
            int maxQ = (int)Math.Min(width - 1L, (long)origin.Q + range);
            int minR = (int)Math.Max(0L, (long)origin.R - range);
            int maxR = (int)Math.Min(height - 1L, (long)origin.R + range);

            for (int r = minR; r <= maxR; r++)
            {
                for (int q = minQ; q <= maxQ; q++)
                {
                    HexCoordinate coordinate = new HexCoordinate(q, r);

                    if (Contains(origin, direction, range, coordinate, minRange))
                    {
                        destination.Add(coordinate);
                    }
                }
            }
        }
    }
}
