using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace RaggedBanner.GameState.Tests
{
    public sealed class EventStateTests
    {
        [Test]
        public void EmptyPoolAndMissingLocationHaveEmptyReadOnlyCandidates()
        {
            EventState state = CreateState(Array.Empty<string>());

            Assert.That(state.RemainingEventIds, Is.Empty);
            Assert.That(state.PotentialEventIdsByLocation, Is.Empty);
            Assert.That(state.ArcStates, Is.Empty);
            Assert.That(state.RevealedGoalIds, Is.Empty);
            Assert.That(state.ResolvedEventCount, Is.Zero);
            Assert.That(state.GetPotentialEventIds(new LocationKey(1, 1)), Is.Empty);
        }

        [Test]
        public void CompletingSharedEventConsumesItGloballyAndPreservesPotentialLists()
        {
            LocationKey first = new LocationKey(1, 1);
            LocationKey second = new LocationKey(2, 1);
            EventState state = CreateState(new[] { "E1", "E2" }, new Dictionary<LocationKey, string[]>
            {
                { first, new[] { "E1", "E2" } },
                { second, new[] { "E1" } }
            });
            IReadOnlyList<string> firstCandidates = state.GetPotentialEventIds(first);
            IReadOnlyList<string> secondCandidates = state.GetPotentialEventIds(second);

            Assert.That(state.RecordRegularEventCompletion("E1"), Is.True);

            Assert.That(state.IsRemaining("E1"), Is.False);
            Assert.That(state.IsRemaining("E2"), Is.True);
            Assert.That(state.RemainingEventIds, Is.EquivalentTo(new[] { "E2" }));
            Assert.That(state.GetPotentialEventIds(first), Is.SameAs(firstCandidates));
            Assert.That(state.GetPotentialEventIds(second), Is.SameAs(secondCandidates));
            Assert.That(firstCandidates, Is.EqualTo(new[] { "E1", "E2" }));
            Assert.That(secondCandidates, Is.EqualTo(new[] { "E1" }));
            Assert.That(state.ResolvedEventCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateAndUnknownCompletionsDoNotIncreaseCount()
        {
            EventState state = CreateState(new[] { "E1" });

            Assert.That(state.RecordRegularEventCompletion("unknown"), Is.False);
            Assert.That(state.RecordRegularEventCompletion("E1"), Is.True);
            Assert.That(state.RecordRegularEventCompletion("E1"), Is.False);
            Assert.That(state.RecordRegularEventCompletion("unknown"), Is.False);
            Assert.That(state.ResolvedEventCount, Is.EqualTo(1));
        }

        [Test]
        public void SpecialEventCompletionsIncreaseCountWithoutConsumingRegularEvents()
        {
            EventState state = CreateState(new[] { "E1" });

            state.RecordSpecialEventCompletion();
            state.RecordSpecialEventCompletion();

            Assert.That(state.ResolvedEventCount, Is.EqualTo(2));
            Assert.That(state.IsRemaining("E1"), Is.True);
            Assert.That(state.RecordRegularEventCompletion("E1"), Is.True);
            Assert.That(state.ResolvedEventCount, Is.EqualTo(3));
        }

        [Test]
        public void CompletionCountOverflowPreservesCountAndRemainingEvents()
        {
            EventState state = CreateState(new[] { "E1" });
            FieldInfo countField = typeof(EventState).GetField("_resolvedEventCount", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(countField, Is.Not.Null);
            countField.SetValue(state, int.MaxValue);

            Assert.Throws<OverflowException>(() => state.RecordRegularEventCompletion("E1"));
            Assert.That(state.IsRemaining("E1"), Is.True);
            Assert.That(state.ResolvedEventCount, Is.EqualTo(int.MaxValue));
            Assert.Throws<OverflowException>(() => state.RecordSpecialEventCompletion());
            Assert.That(state.IsRemaining("E1"), Is.True);
            Assert.That(state.ResolvedEventCount, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void ConstructorCopiesInputsAndPublicViewsCannotMutateState()
        {
            LocationKey location = new LocationKey(1, 1);
            string[] regularIds = { "E1", "E2" };
            string[] potentialIds = { "E1" };
            string[] goals = { "G1" };
            Dictionary<LocationKey, string[]> candidates = new Dictionary<LocationKey, string[]>
            {
                { location, potentialIds }
            };
            Dictionary<string, string> arcs = new Dictionary<string, string>
            {
                { "A1", "start" }
            };
            EventState state = new EventState(regularIds, candidates, arcs, goals);

            regularIds[0] = "changed";
            potentialIds[0] = "E2";
            goals[0] = "changed";
            candidates.Clear();
            arcs["A1"] = "changed";

            Assert.That(state.IsRemaining("E1"), Is.True);
            Assert.That(state.GetPotentialEventIds(location), Is.EqualTo(new[] { "E1" }));
            Assert.That(state.IsGoalRevealed("G1"), Is.True);
            Assert.That(state.ArcStates["A1"].Stage, Is.EqualTo("start"));
            Assert.That(state.RemainingEventIds, Is.Not.InstanceOf<ICollection<string>>());
            Assert.That(state.RevealedGoalIds, Is.Not.InstanceOf<ICollection<string>>());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)state.GetPotentialEventIds(location))[0] = "E2");
            Assert.Throws<NotSupportedException>(() => ((IDictionary<LocationKey, IReadOnlyList<string>>)state.PotentialEventIdsByLocation).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, ArcState>)state.ArcStates).Clear());
        }

        [Test]
        public void RegularPoolRejectsDuplicateAndInvalidIds()
        {
            Assert.Throws<ArgumentException>(() => CreateState(new[] { "E1", "E1" }));
            Assert.Throws<ArgumentException>(() => CreateState(new[] { "E1", null }));
            Assert.Throws<ArgumentException>(() => CreateState(new[] { " " }));
        }

        [Test]
        public void PotentialListsRejectDuplicatesUnknownIdsAndNullLists()
        {
            LocationKey location = new LocationKey(1, 1);

            Assert.Throws<ArgumentException>(() => CreateState(new[] { "E1" }, new Dictionary<LocationKey, string[]>
            {
                { location, new[] { "E1", "E1" } }
            }));
            Assert.Throws<ArgumentException>(() => CreateState(new[] { "E1" }, new Dictionary<LocationKey, string[]>
            {
                { location, new[] { "unknown" } }
            }));
            Assert.Throws<ArgumentException>(() => CreateState(new[] { "E1" }, new Dictionary<LocationKey, string[]>
            {
                { location, null }
            }));
        }

        [Test]
        public void ConstructorRequiresAllInitialCollections()
        {
            string[] emptyIds = Array.Empty<string>();
            Dictionary<LocationKey, string[]> candidates = new Dictionary<LocationKey, string[]>();
            Dictionary<string, string> arcs = new Dictionary<string, string>();

            Assert.Throws<ArgumentNullException>(() => new EventState(null, candidates, arcs, emptyIds));
            Assert.Throws<ArgumentNullException>(() => new EventState(emptyIds, null, arcs, emptyIds));
            Assert.Throws<ArgumentNullException>(() => new EventState(emptyIds, candidates, null, emptyIds));
            Assert.Throws<ArgumentNullException>(() => new EventState(emptyIds, candidates, arcs, null));
        }

        [Test]
        public void EventArcAndGoalIdsUseOrdinalComparison()
        {
            EventState state = new EventState(
                new[] { "E1", "e1" },
                new Dictionary<LocationKey, string[]>(),
                new Dictionary<string, string> { { "A1", "start" }, { "a1", "other" } },
                new[] { "G1", "G1" });

            Assert.That(state.RecordRegularEventCompletion("E1"), Is.True);
            Assert.That(state.IsRemaining("e1"), Is.True);
            Assert.That(state.ArcStates.Count, Is.EqualTo(2));
            Assert.That(state.IsGoalRevealed("g1"), Is.False);
            Assert.That(state.RevealGoal("G1"), Is.False);
            Assert.That(state.RevealGoal("g1"), Is.True);
            Assert.That(state.RevealedGoalIds.Count, Is.EqualTo(2));
        }

        [Test]
        public void ArcStartsPendingAndChangesReturnPreviousAndCurrentValues()
        {
            EventState state = CreateArcState();

            Assert.That(state.ArcStates["A1"].Status, Is.EqualTo(ArcStatus.Pending));
            Assert.That(state.ArcStates["A1"].Stage, Is.EqualTo("start"));
            Assert.That(state.SetArcState("A1", new ArcState("start", ArcStatus.Pending)), Is.Null);

            ArcChangeRecord? change = state.SetArcState("A1", new ArcState("next", ArcStatus.Active));

            Assert.That(change.HasValue, Is.True);
            Assert.That(change.Value.ArcId, Is.EqualTo("A1"));
            Assert.That(change.Value.Before.Stage, Is.EqualTo("start"));
            Assert.That(change.Value.Before.Status, Is.EqualTo(ArcStatus.Pending));
            Assert.That(change.Value.After.Stage, Is.EqualTo("next"));
            Assert.That(change.Value.After.Status, Is.EqualTo(ArcStatus.Active));
            Assert.That(state.ArcStates["A1"].Stage, Is.EqualTo("next"));
            Assert.Throws<InvalidOperationException>(() => state.SetArcState("A1", new ArcState("start", ArcStatus.Pending)));
        }

        [Test]
        public void CompletedArcCannotReactivateOrChangeStageAndDoesNotConsumeRemainingEvents()
        {
            EventState state = CreateArcState();
            state.SetArcState("A1", new ArcState("end", ArcStatus.Completed));

            Assert.That(state.SetArcState("A1", new ArcState("end", ArcStatus.Completed)), Is.Null);
            Assert.Throws<InvalidOperationException>(() => state.SetArcState("A1", new ArcState("next", ArcStatus.Active)));
            Assert.Throws<InvalidOperationException>(() => state.SetArcState("A1", new ArcState("start", ArcStatus.Pending)));
            Assert.Throws<InvalidOperationException>(() => state.SetArcState("A1", new ArcState("other", ArcStatus.Completed)));
            Assert.That(state.ArcStates["A1"].Stage, Is.EqualTo("end"));
            Assert.That(state.IsRemaining("E1"), Is.True);
            Assert.That(state.ResolvedEventCount, Is.Zero);
        }

        [Test]
        public void ArcUpdatesRejectUnknownArcAndDefaultState()
        {
            EventState state = CreateArcState();

            Assert.Throws<ArgumentException>(() => state.SetArcState("unknown", new ArcState("start", ArcStatus.Active)));
            Assert.Throws<ArgumentException>(() => state.SetArcState("A1", default));
            Assert.That(state.ArcStates.Count, Is.EqualTo(1));
            Assert.That(state.ArcStates["A1"].Status, Is.EqualTo(ArcStatus.Pending));
        }

        static EventState CreateState(string[] regularEventIds, Dictionary<LocationKey, string[]> candidates = null)
        {
            return new EventState(
                regularEventIds,
                candidates ?? new Dictionary<LocationKey, string[]>(),
                new Dictionary<string, string>(),
                Array.Empty<string>());
        }

        static EventState CreateArcState()
        {
            return new EventState(
                new[] { "E1" },
                new Dictionary<LocationKey, string[]>(),
                new Dictionary<string, string> { { "A1", "start" } },
                Array.Empty<string>());
        }
    }
}
