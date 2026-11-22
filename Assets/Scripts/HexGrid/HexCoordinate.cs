using System;

namespace RaggedBanner.HexGrid
{
    public readonly struct HexCoordinate : IEquatable<HexCoordinate>
    {
        public int Q { get; }
        public int R { get; }
        public int S => -Q - R;

        public HexCoordinate(int q, int r)
        {
            Q = q;
            R = r;
        }

        public HexCoordinate GetNeighbor(HexDirection direction)
        {
            switch (direction)
            {
                case HexDirection.East:
                    return new HexCoordinate(Q + 1, R);
                case HexDirection.NorthEast:
                    return new HexCoordinate(Q + 1, R - 1);
                case HexDirection.NorthWest:
                    return new HexCoordinate(Q, R - 1);
                case HexDirection.West:
                    return new HexCoordinate(Q - 1, R);
                case HexDirection.SouthWest:
                    return new HexCoordinate(Q - 1, R + 1);
                case HexDirection.SouthEast:
                    return new HexCoordinate(Q, R + 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public int DistanceTo(HexCoordinate other)
        {
            int qDistance = Math.Abs(Q - other.Q);
            int rDistance = Math.Abs(R - other.R);
            int sDistance = Math.Abs(S - other.S);
            return (qDistance + rDistance + sDistance) / 2;
        }

        public bool Equals(HexCoordinate other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }

        public override string ToString()
        {
            return $"({Q}, {R})";
        }

        public static HexCoordinate operator +(HexCoordinate left, HexCoordinate right)
        {
            return new HexCoordinate(left.Q + right.Q, left.R + right.R);
        }

        public static HexCoordinate operator -(HexCoordinate left, HexCoordinate right)
        {
            return new HexCoordinate(left.Q - right.Q, left.R - right.R);
        }

        public static bool operator ==(HexCoordinate left, HexCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoordinate left, HexCoordinate right)
        {
            return !left.Equals(right);
        }
    }
}
