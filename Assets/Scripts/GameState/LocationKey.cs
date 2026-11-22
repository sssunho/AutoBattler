using System;

namespace RaggedBanner.GameState
{
    public readonly struct LocationKey : IEquatable<LocationKey>
    {
        public int AreaId { get; }
        public int NodeId { get; }

        public LocationKey(int areaId, int nodeId)
        {
            AreaId = areaId;
            NodeId = nodeId;
        }

        public bool Equals(LocationKey other)
        {
            return AreaId == other.AreaId && NodeId == other.NodeId;
        }

        public override bool Equals(object obj)
        {
            return obj is LocationKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AreaId * 397) ^ NodeId;
            }
        }

        public static bool operator ==(LocationKey left, LocationKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocationKey left, LocationKey right)
        {
            return !left.Equals(right);
        }
    }
}
