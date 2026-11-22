using System;

namespace RaggedBanner.WorldMap
{
    public readonly struct WorldMapRoad
    {
        public int Id { get; }
        public int LocationAId { get; }
        public int LocationBId { get; }
        public int TravelCost { get; }

        public WorldMapRoad(int id, int locationAId, int locationBId, int travelCost)
        {
            if (travelCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(travelCost));
            }

            Id = id;
            LocationAId = locationAId;
            LocationBId = locationBId;
            TravelCost = travelCost;
        }
    }
}
