using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaggedBanner.WorldMap.Unity
{
    [CreateAssetMenu(menuName = "RaggedBanner/World Map", fileName = "WorldMapDefinition")]
    public sealed class WorldMapDefinition : ScriptableObject
    {
        [SerializeField]
        int _startLocationId = 1;

        [SerializeField, Min(0)]
        int _initialExpeditionTime = 100;

        [SerializeField]
        WorldMapLocationDefinition[] _locations = Array.Empty<WorldMapLocationDefinition>();

        [SerializeField]
        WorldMapRoadDefinition[] _roads = Array.Empty<WorldMapRoadDefinition>();

        public int InitialExpeditionTime => _initialExpeditionTime;
        public int StartLocationId => _startLocationId;
        public IReadOnlyList<WorldMapLocationDefinition> Locations => _locations;
        public IReadOnlyList<WorldMapRoadDefinition> Roads => _roads;

        internal WorldMapGraph CreateGraph()
        {
            if (_locations == null || _roads == null)
            {
                throw new InvalidOperationException("World map locations and roads must be assigned.");
            }

            int[] locationIds = new int[_locations.Length];
            WorldMapRoad[] roads = new WorldMapRoad[_roads.Length];

            for (int i = 0; i < _locations.Length; i++)
            {
                WorldMapLocationDefinition location = _locations[i];

                if (location == null || string.IsNullOrWhiteSpace(location.DisplayName)
                    || !IsNormalized(location.Position.x) || !IsNormalized(location.Position.y))
                {
                    throw new InvalidOperationException("Each location needs a name and a position within the map.");
                }

                locationIds[i] = location.Id;
            }

            for (int i = 0; i < _roads.Length; i++)
            {
                WorldMapRoadDefinition road = _roads[i];

                if (road == null)
                {
                    throw new InvalidOperationException("World map roads cannot be null.");
                }

                roads[i] = new WorldMapRoad(road.Id, road.LocationAId, road.LocationBId, road.TravelCost);
            }

            WorldMapGraph graph = new WorldMapGraph(locationIds, roads);

            if (!graph.ContainsLocation(_startLocationId))
            {
                throw new InvalidOperationException("The starting location must exist in the world map.");
            }

            return graph;
        }

        static bool IsNormalized(float value)
        {
            return value >= 0f && value <= 1f;
        }
    }

    [Serializable]
    public sealed class WorldMapLocationDefinition
    {
        [SerializeField]
        int _id;

        [SerializeField]
        string _displayName;

        [SerializeField]
        Vector2 _position;

        [SerializeField]
        Sprite _icon;

        public int Id => _id;
        public string DisplayName => _displayName;
        public Vector2 Position => _position;
        [SerializeField]
        Vector2 _iconSize = new Vector2(92f, 92f);

        public Sprite Icon => _icon;
        public Vector2 IconSize => _iconSize;
    }

    [Serializable]
    public sealed class WorldMapRoadDefinition
    {
        [SerializeField]
        int _id;

        [SerializeField]
        int _locationAId;

        [SerializeField]
        int _locationBId;

        [SerializeField, Min(0)]
        int _travelCost = 5;

        [SerializeField]
        Vector2 _startOffset;

        [SerializeField]
        Vector2 _endOffset;

        [SerializeField]
        Vector2 _controlOffsetA;

        [SerializeField]
        Vector2 _controlOffsetB;

        [SerializeField, Min(1f)]
        float _width = 22f;

        public int TravelCost => _travelCost;
        public int Id => _id;
        public int LocationAId => _locationAId;
        public int LocationBId => _locationBId;
        internal Vector2 StartOffset => _startOffset;
        internal Vector2 EndOffset => _endOffset;
        internal Vector2 ControlOffsetA => _controlOffsetA;
        internal Vector2 ControlOffsetB => _controlOffsetB;
        internal float Width => _width;

        internal void SetAppearance(Vector2 startOffset, Vector2 endOffset, Vector2 controlOffsetA,
            Vector2 controlOffsetB, float width)
        {
            _startOffset = startOffset;
            _endOffset = endOffset;
            _controlOffsetA = controlOffsetA;
            _controlOffsetB = controlOffsetB;
            _width = width;
        }
    }
}
