using System;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public static class BattleFacing
    {
        public static HexDirection GetDirection(HexCoordinate from, HexCoordinate to)
        {
            if (from == to)
            {
                throw new ArgumentException("Direction requires distinct coordinates.", nameof(to));
            }

            long q = (long)to.Q - from.Q;
            long r = (long)to.R - from.R;
            long bestDot = long.MinValue;
            HexDirection bestDirection = HexDirection.East;

            for (int i = 0; i < 6; i++)
            {
                HexCoordinate direction = default(HexCoordinate).GetNeighbor((HexDirection)i);
                long dot = (2 * q + r) * direction.Q + (q + 2 * r) * direction.R;

                if (dot > bestDot || (dot == bestDot && (i + 1) % 6 == (int)bestDirection))
                {
                    bestDot = dot;
                    bestDirection = (HexDirection)i;
                }
            }

            return bestDirection;
        }

        public static BattleRelativeDirection Classify(HexDirection facing, HexCoordinate defender, HexCoordinate attacker)
        {
            if ((uint)facing >= 6)
            {
                throw new ArgumentOutOfRangeException(nameof(facing));
            }

            if (defender == attacker)
            {
                throw new ArgumentException("Attack direction requires distinct coordinates.", nameof(attacker));
            }

            long q = (long)attacker.Q - defender.Q;
            long r = (long)attacker.R - defender.R;

            if (IsInSector((int)facing, q, r))
            {
                return BattleRelativeDirection.Front;
            }

            if (IsInSector(((int)facing + 3) % 6, q, r))
            {
                return BattleRelativeDirection.Rear;
            }

            return BattleRelativeDirection.Side;
        }

        static bool IsInSector(int facing, long q, long r)
        {
            HexCoordinate origin = default(HexCoordinate).GetNeighbor((HexDirection)facing);
            q -= origin.Q;
            r -= origin.R;
            HexCoordinate left = default(HexCoordinate).GetNeighbor((HexDirection)((facing + 1) % 6));
            HexCoordinate right = default(HexCoordinate).GetNeighbor((HexDirection)((facing + 5) % 6));
            return left.Q * r - left.R * q >= 0 && q * right.R - r * right.Q >= 0;
        }
    }
}
