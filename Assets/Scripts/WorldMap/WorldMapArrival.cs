namespace RaggedBanner.WorldMap
{
    public readonly struct WorldMapArrival
    {
        public int FromLocationId { get; }
        public int ToLocationId { get; }
        public int RoadId { get; }

        public WorldMapArrival(int fromLocationId, int toLocationId, int roadId)
        {
            FromLocationId = fromLocationId;
            ToLocationId = toLocationId;
            RoadId = roadId;
        }
    }
}
