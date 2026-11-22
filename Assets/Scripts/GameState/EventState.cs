using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RaggedBanner.GameState
{
    public sealed class EventState
    {
        static readonly IReadOnlyList<string> _emptyEventIds = Array.AsReadOnly(Array.Empty<string>());

        readonly HashSet<string> _remainingEventIds;
        readonly Dictionary<LocationKey, IReadOnlyList<string>> _potentialEventIdsByLocation;
        readonly Dictionary<string, ArcState> _arcStates;
        readonly HashSet<string> _revealedGoalIds;
        int _resolvedEventCount;

        public IReadOnlyCollection<string> RemainingEventIds { get; }
        public IReadOnlyDictionary<LocationKey, IReadOnlyList<string>> PotentialEventIdsByLocation { get; }
        public IReadOnlyDictionary<string, ArcState> ArcStates { get; }
        public IReadOnlyCollection<string> RevealedGoalIds { get; }
        public int ResolvedEventCount => _resolvedEventCount;

        public EventState(
            IEnumerable<string> regularEventIds,
            IReadOnlyDictionary<LocationKey, string[]> potentialEventIdsByLocation,
            IReadOnlyDictionary<string, string> initialArcStages,
            IEnumerable<string> initialRevealedGoalIds)
        {
            if (regularEventIds == null)
            {
                throw new ArgumentNullException(nameof(regularEventIds));
            }

            if (potentialEventIdsByLocation == null)
            {
                throw new ArgumentNullException(nameof(potentialEventIdsByLocation));
            }

            if (initialArcStages == null)
            {
                throw new ArgumentNullException(nameof(initialArcStages));
            }

            if (initialRevealedGoalIds == null)
            {
                throw new ArgumentNullException(nameof(initialRevealedGoalIds));
            }

            _remainingEventIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string eventId in regularEventIds)
            {
                ValidateId(eventId, nameof(regularEventIds));
                if (!_remainingEventIds.Add(eventId))
                {
                    throw new ArgumentException("Regular event IDs must be unique.", nameof(regularEventIds));
                }
            }

            _potentialEventIdsByLocation = new Dictionary<LocationKey, IReadOnlyList<string>>();
            foreach (KeyValuePair<LocationKey, string[]> entry in potentialEventIdsByLocation)
            {
                if (entry.Value == null)
                {
                    throw new ArgumentException("Potential event lists cannot be null.", nameof(potentialEventIdsByLocation));
                }

                string[] eventIds = new string[entry.Value.Length];
                Array.Copy(entry.Value, eventIds, eventIds.Length);
                HashSet<string> uniqueEventIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < eventIds.Length; i++)
                {
                    string eventId = eventIds[i];
                    ValidateId(eventId, nameof(potentialEventIdsByLocation));
                    if (!_remainingEventIds.Contains(eventId))
                    {
                        throw new ArgumentException("Potential events must belong to the regular event pool.", nameof(potentialEventIdsByLocation));
                    }

                    if (!uniqueEventIds.Add(eventId))
                    {
                        throw new ArgumentException("Potential event IDs must be unique within each location.", nameof(potentialEventIdsByLocation));
                    }
                }

                _potentialEventIdsByLocation.Add(entry.Key, Array.AsReadOnly(eventIds));
            }

            _arcStates = new Dictionary<string, ArcState>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> entry in initialArcStages)
            {
                ValidateId(entry.Key, nameof(initialArcStages));
                ValidateId(entry.Value, nameof(initialArcStages));
                _arcStates.Add(entry.Key, new ArcState(entry.Value, ArcStatus.Pending));
            }

            _revealedGoalIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string goalId in initialRevealedGoalIds)
            {
                ValidateId(goalId, nameof(initialRevealedGoalIds));
                _revealedGoalIds.Add(goalId);
            }

            RemainingEventIds = new ReadOnlySet<string>(_remainingEventIds);
            PotentialEventIdsByLocation = new ReadOnlyDictionary<LocationKey, IReadOnlyList<string>>(_potentialEventIdsByLocation);
            ArcStates = new ReadOnlyDictionary<string, ArcState>(_arcStates);
            RevealedGoalIds = new ReadOnlySet<string>(_revealedGoalIds);
        }

        public IReadOnlyList<string> GetPotentialEventIds(LocationKey location)
        {
            if (_potentialEventIdsByLocation.TryGetValue(location, out IReadOnlyList<string> eventIds))
            {
                return eventIds;
            }

            return _emptyEventIds;
        }

        public bool IsRemaining(string eventId)
        {
            ValidateId(eventId, nameof(eventId));
            return _remainingEventIds.Contains(eventId);
        }

        public bool IsGoalRevealed(string goalId)
        {
            ValidateId(goalId, nameof(goalId));
            return _revealedGoalIds.Contains(goalId);
        }

        public bool RevealGoal(string goalId)
        {
            ValidateId(goalId, nameof(goalId));
            return _revealedGoalIds.Add(goalId);
        }

        public ArcChangeRecord? SetArcState(string arcId, ArcState state)
        {
            ValidateId(arcId, nameof(arcId));
            ValidateId(state.Stage, nameof(state));
            if (state.Status != ArcStatus.Pending && state.Status != ArcStatus.Active && state.Status != ArcStatus.Completed)
            {
                throw new ArgumentException("The arc status is invalid.", nameof(state));
            }

            if (!_arcStates.TryGetValue(arcId, out ArcState before))
            {
                throw new ArgumentException("The arc is not included in this expedition.", nameof(arcId));
            }

            if (before.Status == state.Status && string.Equals(before.Stage, state.Stage, StringComparison.Ordinal))
            {
                return null;
            }

            if (before.Status == ArcStatus.Completed)
            {
                throw new InvalidOperationException("A completed arc cannot change state.");
            }

            if (before.Status == ArcStatus.Active && state.Status == ArcStatus.Pending)
            {
                throw new InvalidOperationException("An active arc cannot return to pending.");
            }

            _arcStates[arcId] = state;
            return new ArcChangeRecord(arcId, before, state);
        }

        public bool RecordRegularEventCompletion(string eventId)
        {
            ValidateId(eventId, nameof(eventId));
            if (!_remainingEventIds.Contains(eventId))
            {
                return false;
            }

            int resolvedEventCount = checked(_resolvedEventCount + 1);
            _remainingEventIds.Remove(eventId);
            _resolvedEventCount = resolvedEventCount;
            return true;
        }

        public void RecordSpecialEventCompletion()
        {
            _resolvedEventCount = checked(_resolvedEventCount + 1);
        }

        static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An ID cannot be null or whitespace.", parameterName);
            }
        }
    }
}
