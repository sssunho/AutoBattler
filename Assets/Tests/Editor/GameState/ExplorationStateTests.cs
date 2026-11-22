using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RaggedBanner.GameState.Tests
{
    public sealed class ExplorationStateTests
    {
        [Test]
        public void InitializationKeepsVisitsAndCandidateHistoryEmpty()
        {
            LocationKey start = new LocationKey(1, 1);
            ExplorationState state = new ExplorationState(start, 100);

            Assert.That(state.CurrentLocation, Is.EqualTo(start));
            Assert.That(state.RemainingExpeditionTime, Is.EqualTo(100));
            Assert.That(state.HasVisited(start), Is.False);
            Assert.That(state.HasSeenEligibleEvent(start, "E1"), Is.False);
            Assert.That(state.VisitedLocations, Is.Empty);
            Assert.That(state.SeenEligibleEventIdsByLocation, Is.Empty);
        }

        [Test]
        public void LocationAndTimeStorageDoNotApplyMovementPolicyOrRecordVisits()
        {
            LocationKey destination = new LocationKey(2, 3);
            ExplorationState state = new ExplorationState(new LocationKey(1, 1), long.MinValue);

            Assert.That(state.RemainingExpeditionTime, Is.EqualTo(long.MinValue));
            state.CurrentLocation = destination;
            state.RemainingExpeditionTime = long.MaxValue;

            Assert.That(state.CurrentLocation, Is.EqualTo(destination));
            Assert.That(state.RemainingExpeditionTime, Is.EqualTo(long.MaxValue));
            Assert.That(state.VisitedLocations, Is.Empty);
        }

        [Test]
        public void FirstVisitIsReportedOnceAndAreaIdsSeparateLocations()
        {
            LocationKey first = new LocationKey(1, 7);
            LocationKey second = new LocationKey(2, 7);
            ExplorationState state = new ExplorationState(first, 100);

            Assert.That(state.RecordVisit(first), Is.True);
            Assert.That(state.RecordVisit(first), Is.False);
            Assert.That(state.HasVisited(second), Is.False);
            Assert.That(state.RecordVisit(second), Is.True);
            state.RecordEligibleEvents(first, new[] { "E1" });

            Assert.That(state.VisitedLocations.Count, Is.EqualTo(2));
            Assert.That(state.HasSeenEligibleEvent(first, "E1"), Is.True);
            Assert.That(state.HasSeenEligibleEvent(second, "E1"), Is.False);
        }

        [Test]
        public void UnselectedCandidatesRemainInHistoryWithoutBeingConsumed()
        {
            LocationKey location = new LocationKey(1, 1);
            ExplorationState exploration = new ExplorationState(location, 100);
            EventState events = new EventState(
                new[] { "E1", "E2", "E3" },
                new Dictionary<LocationKey, string[]> { { location, new[] { "E1", "E2", "E3" } } },
                new Dictionary<string, string>(),
                Array.Empty<string>());

            exploration.RecordVisit(location);
            exploration.RecordEligibleEvents(location, new[] { "E1", "E2" });
            events.RecordRegularEventCompletion("E1");

            Assert.That(exploration.HasSeenEligibleEvent(location, "E1"), Is.True);
            Assert.That(exploration.HasSeenEligibleEvent(location, "E2"), Is.True);
            Assert.That(exploration.HasSeenEligibleEvent(location, "E3"), Is.False);
            Assert.That(events.IsRemaining("E1"), Is.False);
            Assert.That(events.IsRemaining("E2"), Is.True);
            Assert.That(events.IsRemaining("E3"), Is.True);
            Assert.That(exploration.RecordVisit(location), Is.False);
            Assert.That(exploration.SeenEligibleEventIdsByLocation[location], Is.EquivalentTo(new[] { "E1", "E2" }));
        }

        [Test]
        public void RemoteQueriesAndRejectedCandidateRecordsDoNotCreateVisits()
        {
            LocationKey remote = new LocationKey(2, 1);
            ExplorationState state = new ExplorationState(new LocationKey(1, 1), 100);

            Assert.That(state.HasVisited(remote), Is.False);
            Assert.That(state.HasSeenEligibleEvent(remote, "E1"), Is.False);
            Assert.Throws<InvalidOperationException>(() => state.RecordEligibleEvents(remote, new[] { "E1" }));
            Assert.That(state.VisitedLocations, Is.Empty);
            Assert.That(state.SeenEligibleEventIdsByLocation, Is.Empty);
        }

        [Test]
        public void InvalidCandidateListDoesNotPartiallyRecordAnyHistory()
        {
            LocationKey location = new LocationKey(1, 1);
            ExplorationState state = new ExplorationState(location, 100);
            state.RecordVisit(location);

            Assert.Throws<ArgumentNullException>(() => state.RecordEligibleEvents(location, null));
            Assert.Throws<ArgumentException>(() => state.RecordEligibleEvents(location, new[] { "E1", null }));
            Assert.That(state.SeenEligibleEventIdsByLocation, Is.Empty);

            state.RecordEligibleEvents(location, new[] { "E1" });

            Assert.Throws<ArgumentException>(() => state.RecordEligibleEvents(location, new[] { "E2", " " }));
            Assert.That(state.SeenEligibleEventIdsByLocation[location], Is.EqualTo(new[] { "E1" }));
        }

        [Test]
        public void EmptyCandidateListIsValidForVisitedLocation()
        {
            LocationKey location = new LocationKey(1, 1);
            ExplorationState state = new ExplorationState(location, 100);
            state.RecordVisit(location);

            state.RecordEligibleEvents(location, Array.Empty<string>());

            Assert.That(state.HasVisited(location), Is.True);
            Assert.That(state.SeenEligibleEventIdsByLocation, Is.Empty);
        }

        [Test]
        public void CandidateHistoryUsesOrdinalIdsAndReadOnlyLiveViews()
        {
            LocationKey location = new LocationKey(1, 1);
            ExplorationState state = new ExplorationState(location, 100);
            state.RecordVisit(location);
            string[] candidates = { "E1", "E1" };
            state.RecordEligibleEvents(location, candidates);
            IReadOnlyCollection<string> history = state.SeenEligibleEventIdsByLocation[location];
            candidates[0] = "changed";

            Assert.That(state.HasSeenEligibleEvent(location, "changed"), Is.False);
            Assert.That(state.HasSeenEligibleEvent(location, "e1"), Is.False);
            state.RecordEligibleEvents(location, new[] { "e1" });

            Assert.That(history, Is.EquivalentTo(new[] { "E1", "e1" }));
            Assert.That(history, Is.Not.InstanceOf<ICollection<string>>());
            Assert.That(state.VisitedLocations, Is.Not.InstanceOf<ICollection<LocationKey>>());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<LocationKey, IReadOnlyCollection<string>>)state.SeenEligibleEventIdsByLocation).Clear());
        }
    }
}
