using System;
using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.GameState.Tests
{
    public sealed class GameStateTests
    {
        [Test]
        public void NewRunOwnsIndependentStatesAndKeepsExplicitInitialValues()
        {
            var tags = new List<string> { "known", "Known", "known" };
            GameState first = CreateState(initialTags: tags);
            GameState second = CreateState();
            tags.Clear();

            Assert.That(first.Money, Is.EqualTo(20));
            Assert.That(first.Fame, Is.EqualTo(9));
            Assert.That(first.Reputation, Is.EqualTo(-2));
            Assert.That(first.Tags, Has.Count.EqualTo(2));
            Assert.That(first.HasTag("known"), Is.True);
            Assert.That(first.HasTag("KNOWN"), Is.False);
            Assert.That(first.RunPhase, Is.EqualTo(RunPhase.Opening));
            Assert.That(first.RunResult.HasValue, Is.False);
            Assert.That(first.ExplorationState.CurrentLocation, Is.EqualTo(new LocationKey(1, 2)));
            Assert.That(first.ExplorationState.RemainingExpeditionTime, Is.EqualTo(100L));
            Assert.That(first.ExplorationState.VisitedLocations, Is.Empty);
            Assert.That(first.MercenaryState.Experience, Is.EqualTo(7));
            Assert.That(first.EventState.ResolvedEventCount, Is.Zero);
            Assert.That(first.BlessingState.UsedConditionIds, Is.Empty);
            Assert.That(first.Chronicle, Is.Empty);

            first.MercenaryState.AddMercenary("First", MercenaryClass.Warrior, 0);
            first.BlessingState.TryRecordAcquisition("god", "condition");
            first.EventState.RevealGoal("extra-goal");
            first.ExplorationState.RecordVisit(new LocationKey(1, 2));

            Assert.That(second.MercenaryState.Mercenaries, Is.Empty);
            Assert.That(second.BlessingState.TiersByGodId, Is.Empty);
            Assert.That(second.EventState.IsGoalRevealed("extra-goal"), Is.False);
            Assert.That(second.ExplorationState.VisitedLocations, Is.Empty);
        }

        [TestCase(-1, 0, 0)]
        [TestCase(0, -1, 0)]
        [TestCase(0, 11, 0)]
        [TestCase(0, 0, -4)]
        [TestCase(0, 0, 4)]
        public void InvalidInitialResourcesAreRejected(int money, int fame, int reputation)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateState(money, fame, reputation));
        }

        [Test]
        public void ResourceChangesClampWithoutIntegerOverflow()
        {
            GameState state = CreateState(initialMoney: int.MaxValue);

            state.ApplyResourceChanges(-int.MaxValue, int.MaxValue, int.MaxValue);

            Assert.That(state.Money, Is.Zero);
            Assert.That(state.Fame, Is.EqualTo(10));
            Assert.That(state.Reputation, Is.EqualTo(3));

            state.ApplyResourceChanges(0, int.MinValue, int.MinValue);

            Assert.That(state.Fame, Is.Zero);
            Assert.That(state.Reputation, Is.EqualTo(-3));
        }

        [Test]
        public void InvalidMoneyChangeDoesNotPartiallyApplyOtherResources()
        {
            GameState state = CreateState(initialMoney: int.MaxValue);

            Assert.Throws<OverflowException>(() => state.ApplyResourceChanges(1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.ApplyResourceChanges(int.MinValue, 1, 1));
            Assert.That(state.Money, Is.EqualTo(int.MaxValue));
            Assert.That(state.Fame, Is.EqualTo(9));
            Assert.That(state.Reputation, Is.EqualTo(-2));
        }

        [Test]
        public void TagChangesDistinguishActualChangesAndPreserveCase()
        {
            GameState state = CreateState();

            Assert.That(state.AddTag("tag"), Is.True);
            Assert.That(state.AddTag("tag"), Is.False);
            Assert.That(state.AddTag("Tag"), Is.True);
            Assert.That(state.RemoveTag("missing"), Is.False);
            Assert.That(state.RemoveTag("tag"), Is.True);
            Assert.That(state.HasTag("Tag"), Is.True);
            Assert.Throws<ArgumentException>(() => state.AddTag(" "));
            Assert.That(state.Tags, Is.Not.InstanceOf<ICollection<string>>());
        }

        [Test]
        public void CompletingOpeningDoesNotRepeatOrConsumeRegularEvents()
        {
            GameState state = CreateState();

            Assert.That(state.CompleteOpening(), Is.True);
            Assert.That(state.CompleteOpening(), Is.False);
            Assert.That(state.RunPhase, Is.EqualTo(RunPhase.Normal));
            Assert.That(state.RunResult.HasValue, Is.False);
            Assert.That(state.EventState.IsRemaining("event"), Is.True);
        }

        [TestCase(RunOutcome.GoalSuccess, "goal")]
        [TestCase(RunOutcome.BattleDefeat, null)]
        public void EndingRunPreservesFirstResultAgainstLaterTimeExpiry(RunOutcome outcome, string goalId)
        {
            GameState state = CreateState();

            Assert.That(state.TryEndRun(new RunResult(outcome, goalId)), Is.True);
            Assert.That(state.TryEndRun(new RunResult(RunOutcome.TimeExpired)), Is.False);
            Assert.That(state.CompleteOpening(), Is.False);
            Assert.That(state.RunPhase, Is.EqualTo(RunPhase.Ended));
            Assert.That(state.RunResult.Value.Outcome, Is.EqualTo(outcome));
            Assert.That(state.RunResult.Value.SucceededGoalId, Is.EqualTo(goalId));
            Assert.That(state.EventState.ArcStates["arc"].Status, Is.EqualTo(ArcStatus.Pending));
        }

        [Test]
        public void EmptyRegularPoolDoesNotEndRun()
        {
            GameState state = CreateState();

            state.EventState.RecordRegularEventCompletion("event");

            Assert.That(state.EventState.RemainingEventIds, Is.Empty);
            Assert.That(state.RunResult.HasValue, Is.False);
            Assert.That(state.RunPhase, Is.EqualTo(RunPhase.Opening));
        }

        [Test]
        public void InvalidOrDefaultRunResultCannotEndRun()
        {
            GameState state = CreateState();

            Assert.Throws<ArgumentException>(() => new RunResult(RunOutcome.GoalSuccess));
            Assert.Throws<ArgumentException>(() => new RunResult(RunOutcome.GoalSuccess, " "));
            Assert.Throws<ArgumentException>(() => new RunResult(RunOutcome.BattleDefeat, "goal"));
            Assert.Throws<ArgumentException>(() => new RunResult(RunOutcome.TimeExpired, "goal"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunResult((RunOutcome)100));
            Assert.Throws<ArgumentException>(() => state.TryEndRun(default));
            Assert.That(state.RunResult.HasValue, Is.False);
            Assert.That(state.RunPhase, Is.EqualTo(RunPhase.Opening));
        }

        [Test]
        public void ChronicleKeepsCopiedEffectsAndActualClampedDelta()
        {
            GameState state = CreateState();
            int previousFame = state.Fame;
            state.ApplyResourceChanges(0, 3, 0);
            var addedTags = new[] { "tag" };
            var removedTags = new[] { "old-tag" };
            var revealedGoals = new[] { "goal" };
            var effects = new AppliedEventEffects(addedTags, removedTags,
                state.Fame - previousFame, 0, 0, revealedGoals, null);
            var entry = new ChronicleEntry("event", "auto", new LocationKey(1, 2), null, effects);

            state.AppendChronicle(entry);
            addedTags[0] = "changed";
            removedTags[0] = "changed";
            revealedGoals[0] = "changed";

            Assert.That(state.Chronicle[0].ChoiceId, Is.EqualTo("auto"));
            Assert.That(state.Chronicle[0].AppliedEffects.FameChange, Is.EqualTo(1));
            Assert.That(effects.AddedTags, Is.EqualTo(new[] { "tag" }));
            Assert.That(effects.RemovedTags, Is.EqualTo(new[] { "old-tag" }));
            Assert.That(effects.RevealedGoalIds, Is.EqualTo(new[] { "goal" }));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)effects.AddedTags)[0] = "changed");
            Assert.Throws<NotSupportedException>(() => ((IList<ChronicleEntry>)state.Chronicle).Clear());
            Assert.Throws<ArgumentNullException>(() => state.AppendChronicle(null));
        }

        [Test]
        public void DefeatRecordRejectsUnappliedEffects()
        {
            var effects = new AppliedEventEffects(Array.Empty<string>(), Array.Empty<string>(),
                1, 0, 0, Array.Empty<string>(), null);

            Assert.Throws<ArgumentException>(() => new ChronicleEntry("battle", "fight",
                new LocationKey(1, 2), EventBattleOutcome.Defeat, effects));
            var defeat = new ChronicleEntry("battle", "fight", new LocationKey(1, 2),
                EventBattleOutcome.Defeat, AppliedEventEffects.Empty);

            Assert.That(defeat.AppliedEffects.IsEmpty, Is.True);
            Assert.That(defeat.BattleOutcome, Is.EqualTo(EventBattleOutcome.Defeat));
        }

        [Test]
        public void SummaryCopiesFinalValuesAndSurvivesLaterStateChanges()
        {
            GameState state = CreateState(initialTags: new[] { "tag" });
            var entry = new ChronicleEntry("event", "auto", new LocationKey(1, 2), null,
                AppliedEventEffects.Empty);
            state.AppendChronicle(entry);
            Assert.Throws<InvalidOperationException>(() => state.CreateRunSummary());
            state.TryEndRun(new RunResult(RunOutcome.TimeExpired));

            RunSummary summary = state.CreateRunSummary();
            state.ExplorationState.CurrentLocation = new LocationKey(2, 3);
            state.ApplyResourceChanges(-5, -4, 3);
            state.RemoveTag("tag");
            state.EventState.SetArcState("arc", new ArcState("next", ArcStatus.Active));
            state.AppendChronicle(entry);

            Assert.That(summary.Result.Outcome, Is.EqualTo(RunOutcome.TimeExpired));
            Assert.That(summary.LastLocation, Is.EqualTo(new LocationKey(1, 2)));
            Assert.That(summary.Money, Is.EqualTo(20));
            Assert.That(summary.Fame, Is.EqualTo(9));
            Assert.That(summary.Reputation, Is.EqualTo(-2));
            Assert.That(summary.Tags, Is.EqualTo(new[] { "tag" }));
            Assert.That(summary.ArcStates["arc"].Stage, Is.EqualTo("start"));
            Assert.That(summary.ArcStates["arc"].Status, Is.EqualTo(ArcStatus.Pending));
            Assert.That(summary.Chronicle, Has.Count.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)summary.Tags).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, ArcState>)summary.ArcStates).Clear());
        }

        [Test]
        public void LocationIdentityIncludesAreaAndNode()
        {
            var location = new LocationKey(1, 2);
            var same = new LocationKey(1, 2);
            var anotherArea = new LocationKey(2, 2);
            var anotherNode = new LocationKey(1, 3);
            var locations = new HashSet<LocationKey> { location, same, anotherArea, anotherNode };

            Assert.That(location == same, Is.True);
            Assert.That(location != anotherArea, Is.True);
            Assert.That(location.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(locations, Has.Count.EqualTo(3));
        }

        static GameState CreateState(int initialMoney = 20, int initialFame = 9,
            int initialReputation = -2, IEnumerable<string> initialTags = null)
        {
            var location = new LocationKey(1, 2);
            return new GameState(location, 100L, initialMoney, initialFame, initialReputation,
                initialTags ?? Array.Empty<string>(), 7, new[] { "event" },
                new Dictionary<LocationKey, string[]> { { location, new[] { "event" } } },
                new Dictionary<string, string> { { "arc", "start" } }, new[] { "goal" });
        }
    }
}
