using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RaggedBanner.GameState
{
    public sealed class ExplorationState
    {
        readonly HashSet<LocationKey> _visitedLocations;
        readonly Dictionary<LocationKey, HashSet<string>> _seenEligibleEventIdsByLocation;
        readonly Dictionary<LocationKey, IReadOnlyCollection<string>> _seenEligibleEventViewsByLocation;

        public LocationKey CurrentLocation { get; set; }
        public long RemainingExpeditionTime { get; set; }
        public IReadOnlyCollection<LocationKey> VisitedLocations { get; }
        public IReadOnlyDictionary<LocationKey, IReadOnlyCollection<string>> SeenEligibleEventIdsByLocation { get; }

        public ExplorationState(LocationKey startLocation, long initialTime)
        {
            CurrentLocation = startLocation;
            RemainingExpeditionTime = initialTime;
            _visitedLocations = new HashSet<LocationKey>();
            _seenEligibleEventIdsByLocation = new Dictionary<LocationKey, HashSet<string>>();
            _seenEligibleEventViewsByLocation = new Dictionary<LocationKey, IReadOnlyCollection<string>>();
            VisitedLocations = new ReadOnlySet<LocationKey>(_visitedLocations);
            SeenEligibleEventIdsByLocation = new ReadOnlyDictionary<LocationKey, IReadOnlyCollection<string>>(_seenEligibleEventViewsByLocation);
        }

        public bool HasVisited(LocationKey location)
        {
            return _visitedLocations.Contains(location);
        }

        public bool RecordVisit(LocationKey location)
        {
            return _visitedLocations.Add(location);
        }

        public bool HasSeenEligibleEvent(LocationKey location, string eventId)
        {
            ValidateEventId(eventId, nameof(eventId));
            return _seenEligibleEventIdsByLocation.TryGetValue(location, out HashSet<string> eventIds)
                && eventIds.Contains(eventId);
        }

        public void RecordEligibleEvents(LocationKey location, IReadOnlyList<string> eligibleEventIds)
        {
            if (eligibleEventIds == null)
            {
                throw new ArgumentNullException(nameof(eligibleEventIds));
            }

            for (int i = 0; i < eligibleEventIds.Count; i++)
            {
                ValidateEventId(eligibleEventIds[i], nameof(eligibleEventIds));
            }

            if (!_visitedLocations.Contains(location))
            {
                throw new InvalidOperationException("Eligible events can only be recorded for a visited location.");
            }

            if (eligibleEventIds.Count == 0)
            {
                return;
            }

            if (!_seenEligibleEventIdsByLocation.TryGetValue(location, out HashSet<string> eventIds))
            {
                eventIds = new HashSet<string>(StringComparer.Ordinal);
                _seenEligibleEventIdsByLocation.Add(location, eventIds);
                _seenEligibleEventViewsByLocation.Add(location, new ReadOnlySet<string>(eventIds));
            }

            for (int i = 0; i < eligibleEventIds.Count; i++)
            {
                eventIds.Add(eligibleEventIds[i]);
            }
        }

        static void ValidateEventId(string eventId, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException("An event ID cannot be null or whitespace.", parameterName);
            }
        }
    }
}
