using System;
using RaggedBanner.GameState;

namespace RaggedBanner.WorldMap
{
    public sealed class WorldMapTravel
    {
        readonly WorldMapGraph _graph;
        readonly ExplorationState _explorationState;
        int _destinationLocationId;
        WorldMapRoad _activeRoad;

        public long RemainingTime => _explorationState.RemainingExpeditionTime;
        public int CurrentLocationId => _explorationState.CurrentLocation.NodeId;
        public bool IsMoving => _destinationLocationId != 0;
        public int DestinationLocationId => _destinationLocationId;
        public WorldMapRoad ActiveRoad => _activeRoad;

        public WorldMapTravel(WorldMapGraph graph, int startLocationId, int initialTime)
            : this(graph, new ExplorationState(new LocationKey(1, startLocationId), initialTime))
        {
        }

        public WorldMapTravel(WorldMapGraph graph, ExplorationState explorationState)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (explorationState == null)
            {
                throw new ArgumentNullException(nameof(explorationState));
            }

            if (explorationState.CurrentLocation.AreaId <= 0
                || !graph.ContainsLocation(explorationState.CurrentLocation.NodeId))
            {
                throw new ArgumentException("The current location must belong to a positive area and exist in the graph.", nameof(explorationState));
            }

            if (explorationState.RemainingExpeditionTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(explorationState));
            }

            _graph = graph;
            _explorationState = explorationState;
        }

        public bool CanTravelTo(int locationId)
        {
            return !IsMoving && RemainingTime > 0 && locationId != CurrentLocationId
                && _graph.TryGetRoad(CurrentLocationId, locationId, out _);
        }

        public bool TryBeginTravel(int locationId)
        {
            if (IsMoving || RemainingTime <= 0 || locationId == CurrentLocationId
                || !_graph.TryGetRoad(CurrentLocationId, locationId, out WorldMapRoad road))
            {
                return false;
            }

            _explorationState.RemainingExpeditionTime -= road.TravelCost;
            _destinationLocationId = locationId;
            _activeRoad = road;
            return true;
        }

        public bool TryCompleteTravel(out WorldMapArrival arrival)
        {
            if (!IsMoving)
            {
                arrival = default;
                return false;
            }

            arrival = new WorldMapArrival(CurrentLocationId, _destinationLocationId, _activeRoad.Id);
            _explorationState.CurrentLocation = new LocationKey(_explorationState.CurrentLocation.AreaId, _destinationLocationId);
            _destinationLocationId = 0;
            _activeRoad = default;
            return true;
        }
    }
}
