using System;
using System.Collections.Generic;

namespace RaggedBanner.WorldMap
{
    public sealed class WorldMapGraph
    {
        readonly HashSet<int> _locations = new HashSet<int>();
        readonly Dictionary<long, WorldMapRoad> _roads = new Dictionary<long, WorldMapRoad>();

        public WorldMapGraph(IReadOnlyList<int> locationIds, IReadOnlyList<WorldMapRoad> roads)
        {
            if (locationIds == null || locationIds.Count == 0)
            {
                throw new ArgumentException("A world map must contain locations.", nameof(locationIds));
            }

            if (roads == null)
            {
                throw new ArgumentNullException(nameof(roads));
            }

            for (int i = 0; i < locationIds.Count; i++)
            {
                if (locationIds[i] <= 0 || !_locations.Add(locationIds[i]))
                {
                    throw new ArgumentException("Location IDs must be positive and unique.", nameof(locationIds));
                }
            }

            HashSet<int> roadIds = new HashSet<int>();

            for (int i = 0; i < roads.Count; i++)
            {
                WorldMapRoad road = roads[i];

                if (road.Id <= 0 || !roadIds.Add(road.Id)
                    || road.LocationAId == road.LocationBId
                    || !_locations.Contains(road.LocationAId) || !_locations.Contains(road.LocationBId))
                {
                    throw new ArgumentException("Road IDs must be unique and connect two existing locations.", nameof(roads));
                }

                long key = GetRoadKey(road.LocationAId, road.LocationBId);

                if (_roads.ContainsKey(key))
                {
                    throw new ArgumentException("Only one road may connect each pair of locations.", nameof(roads));
                }

                _roads.Add(key, road);
            }
        }

        public bool ContainsLocation(int locationId)
        {
            return _locations.Contains(locationId);
        }

        public bool TryGetRoad(int fromLocationId, int toLocationId, out WorldMapRoad road)
        {
            return _roads.TryGetValue(GetRoadKey(fromLocationId, toLocationId), out road);
        }

        static long GetRoadKey(int a, int b)
        {
            return ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
        }
    }
}
