using System;
using System.Collections.Generic;
using RaggedBanner.GameState;
using ExpeditionState = RaggedBanner.GameState.GameState;

namespace RaggedBanner.Events
{
    public sealed class EventCatalog
    {
        readonly Dictionary<string, EventDefinition> _eventsById;
        readonly Dictionary<int, EventDefinition> _regularEventsByLocation;
        readonly HashSet<int> _locationIds;

        public IReadOnlyList<EventDefinition> Events { get; }
        public EventDefinition Opening { get; }
        public int StartLocationId { get; }

        EventCatalog(List<EventDefinition> events, Dictionary<string, EventDefinition> eventsById,
            Dictionary<int, EventDefinition> regularEventsByLocation, HashSet<int> locationIds,
            EventDefinition opening, int startLocationId)
        {
            Events = events.AsReadOnly();
            _eventsById = eventsById;
            _regularEventsByLocation = regularEventsByLocation;
            _locationIds = locationIds;
            Opening = opening;
            StartLocationId = startLocationId;
        }

        public static EventCatalog Load(IReadOnlyDictionary<string, string> sources,
            IReadOnlyCollection<int> locationIds, int startLocationId)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            if (locationIds == null)
            {
                throw new ArgumentNullException(nameof(locationIds));
            }

            var locations = new HashSet<int>();
            foreach (int locationId in locationIds)
            {
                if (locationId <= 0 || !locations.Add(locationId))
                {
                    throw new ArgumentException("Location IDs must be positive and unique.", nameof(locationIds));
                }
            }

            if (!locations.Contains(startLocationId))
            {
                throw new ArgumentException("The start location must exist in the location catalog.", nameof(startLocationId));
            }

            var events = new List<EventDefinition>();
            var byId = new Dictionary<string, EventDefinition>(StringComparer.Ordinal);
            var byLocation = new Dictionary<int, EventDefinition>();
            EventDefinition opening = null;
            foreach (KeyValuePair<string, string> source in sources)
            {
                if (string.IsNullOrWhiteSpace(source.Key))
                {
                    throw new ArgumentException("Each event source must have a name.", nameof(sources));
                }

                EventDefinition definition = EventJsonReader.Read(source.Key, source.Value);
                if (byId.ContainsKey(definition.Id))
                {
                    throw new FormatException(source.Key + ": Duplicate event ID " + definition.Id + ".");
                }

                for (int i = 0; i < definition.Conditions.Locations.Count; i++)
                {
                    int locationId = definition.Conditions.Locations[i];
                    if (!locations.Contains(locationId))
                    {
                        throw new FormatException(source.Key + ": Unknown location ID " + locationId + ".");
                    }

                    if (definition.EventType == EventType.Normal)
                    {
                        if (byLocation.ContainsKey(locationId))
                        {
                            throw new FormatException(source.Key + ": This prototype supports one regular event per location; conflict at " + locationId + ".");
                        }

                        byLocation.Add(locationId, definition);
                    }
                }

                if (definition.EventType == EventType.Opening)
                {
                    if (opening != null || definition.Conditions.Locations.Count != 1
                        || definition.Conditions.Locations[0] != startLocationId)
                    {
                        throw new FormatException(source.Key + ": Exactly one Opening must target only the start location.");
                    }

                    opening = definition;
                }

                events.Add(definition);
                byId.Add(definition.Id, definition);
            }

            if (opening == null)
            {
                throw new FormatException("The event catalog requires exactly one Opening.");
            }

            return new EventCatalog(events, byId, byLocation, locations, opening, startLocationId);
        }

        public EventDefinition GetById(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException("An event ID is required.", nameof(eventId));
            }

            if (!_eventsById.TryGetValue(eventId, out EventDefinition definition))
            {
                throw new KeyNotFoundException("Unknown event ID " + eventId + ".");
            }

            return definition;
        }

        public ExpeditionState CreateGameState(int areaId, long initialTime, int initialMoney,
            int initialFame = 0, int initialReputation = 0)
        {
            if (areaId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(areaId));
            }

            if (initialTime < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialTime));
            }

            var regularIds = new List<string>();
            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i].EventType == EventType.Normal)
                {
                    regularIds.Add(Events[i].Id);
                }
            }

            var potentialIds = new Dictionary<LocationKey, string[]>();
            foreach (KeyValuePair<int, EventDefinition> entry in _regularEventsByLocation)
            {
                potentialIds.Add(new LocationKey(areaId, entry.Key), new[] { entry.Value.Id });
            }

            var state = new ExpeditionState(new LocationKey(areaId, StartLocationId), initialTime,
                initialMoney, initialFame, initialReputation, Array.Empty<string>(), 0, regularIds,
                potentialIds, new Dictionary<string, string>(), Array.Empty<string>());
            if (!Opening.Conditions.Matches(state, StartLocationId))
            {
                throw new InvalidOperationException("The Opening conditions do not match the initial expedition state.");
            }

            if (_regularEventsByLocation.TryGetValue(StartLocationId, out EventDefinition regular)
                && regular.Conditions.Matches(state, StartLocationId))
            {
                throw new InvalidOperationException("A regular event cannot be initially eligible at the Opening location.");
            }

            return state;
        }

        internal bool ContainsLocation(int locationId)
        {
            return _locationIds.Contains(locationId);
        }
    }
}
