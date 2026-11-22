using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.GameState.Tests
{
    public sealed class MercenaryStateTests
    {
        [Test]
        public void NewRunStartsAtLevelOneAndRetainsExplicitExperience()
        {
            var state = new MercenaryState(7);

            Assert.That(state.Level, Is.EqualTo(1));
            Assert.That(state.Experience, Is.EqualTo(7));
            Assert.That(state.NextInstanceId, Is.EqualTo(1));
            Assert.That(state.Mercenaries, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => new MercenaryState(-1));
        }

        [Test]
        public void RemovedIdsAreNotReusedAndRemainingOrderIsPreserved()
        {
            var state = new MercenaryState(0);
            MercenaryInstance first = state.AddMercenary("First", MercenaryClass.Warrior, 10);
            MercenaryInstance second = state.AddMercenary("Second", MercenaryClass.Warrior, 0);

            Assert.That(state.TryRemoveMercenary(first.InstanceId), Is.True);
            Assert.That(state.TryRemoveMercenary(first.InstanceId), Is.False);
            MercenaryInstance third = state.AddMercenary("Third", MercenaryClass.Rogue, 8);

            Assert.That(first.InstanceId, Is.EqualTo(1));
            Assert.That(second.InstanceId, Is.EqualTo(2));
            Assert.That(third.InstanceId, Is.EqualTo(3));
            Assert.That(state.NextInstanceId, Is.EqualTo(4));
            Assert.That(state.Mercenaries, Is.EqualTo(new[] { second, third }));
        }

        [Test]
        public void InvalidMercenaryInputDoesNotConsumeIdOrChangeRoster()
        {
            var state = new MercenaryState(0);

            Assert.Throws<ArgumentException>(() => state.AddMercenary(null, MercenaryClass.Warrior, 10));
            Assert.Throws<ArgumentException>(() => state.AddMercenary(" ", MercenaryClass.Warrior, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.AddMercenary("Name", (MercenaryClass)100, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.AddMercenary("Name", MercenaryClass.Warrior, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.TryRemoveMercenary(0));
            Assert.That(state.Mercenaries, Is.Empty);
            Assert.That(state.NextInstanceId, Is.EqualTo(1));
        }

        [Test]
        public void InstanceIdOverflowLeavesRosterAndNextIdUnchanged()
        {
            var state = new MercenaryState(0);
            FieldInfo nextId = typeof(MercenaryState).GetField("_nextInstanceId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(nextId, Is.Not.Null);
            nextId.SetValue(state, int.MaxValue - 1);

            MercenaryInstance last = state.AddMercenary("Last", MercenaryClass.Archer, 8);

            Assert.That(last.InstanceId, Is.EqualTo(int.MaxValue - 1));
            Assert.That(state.NextInstanceId, Is.EqualTo(int.MaxValue));
            Assert.Throws<OverflowException>(() => state.AddMercenary("Overflow", MercenaryClass.Archer, 8));
            Assert.That(state.Mercenaries, Is.EqualTo(new[] { last }));
            Assert.That(state.NextInstanceId, Is.EqualTo(int.MaxValue));
        }

        [TestCase(0, 0)]
        [TestCase(6, 0)]
        [TestCase(2, -1)]
        public void InvalidProgressDoesNotPartiallyChangeSharedValues(int level, int experience)
        {
            var state = new MercenaryState(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => state.SetProgress(level, experience));
            Assert.That(state.Level, Is.EqualTo(1));
            Assert.That(state.Experience, Is.EqualTo(3));
        }

        [Test]
        public void SharedProgressDoesNotResetZeroHpOrIndividualSelections()
        {
            var state = new MercenaryState(0);
            MercenaryInstance first = state.AddMercenary("First", MercenaryClass.Warrior, 10);
            MercenaryInstance second = state.AddMercenary("Second", MercenaryClass.Warrior, 4);
            first.SelectSpecialization("guardian");
            first.SetSelectedReward("level-3", "sturdy");
            first.SetCurrentHP(0);

            state.SetProgress(5, int.MaxValue);

            Assert.That(state.Level, Is.EqualTo(5));
            Assert.That(state.Experience, Is.EqualTo(int.MaxValue));
            Assert.That(state.Mercenaries, Has.Count.EqualTo(2));
            Assert.That(first.CurrentHP, Is.Zero);
            Assert.That(first.SpecializationId, Is.EqualTo("guardian"));
            Assert.That(first.SelectedRewardIds["level-3"], Is.EqualTo("sturdy"));
            Assert.That(second.CurrentHP, Is.EqualTo(4));
            Assert.That(second.SpecializationId, Is.Null);
            Assert.That(second.SelectedRewardIds, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => first.SetCurrentHP(-1));
            Assert.That(first.CurrentHP, Is.Zero);
        }

        [Test]
        public void SpecializationRemainsFixedAndRewardRecordsCanBeReplaced()
        {
            var state = new MercenaryState(0);
            MercenaryInstance mercenary = state.AddMercenary("Name", MercenaryClass.Warrior, 10);

            mercenary.SelectSpecialization("guardian");
            mercenary.SetSelectedReward("slot", "reward-a");
            mercenary.SetSelectedReward("Slot", "reward-b");

            Assert.Throws<InvalidOperationException>(() => mercenary.SelectSpecialization("guardian"));
            Assert.Throws<InvalidOperationException>(() => mercenary.SelectSpecialization("pioneer"));
            mercenary.SetSelectedReward("slot", "reward-c");
            Assert.That(mercenary.SpecializationId, Is.EqualTo("guardian"));
            Assert.That(mercenary.SelectedRewardIds, Has.Count.EqualTo(2));
            Assert.That(mercenary.SelectedRewardIds["slot"], Is.EqualTo("reward-c"));
            Assert.That(mercenary.SelectedRewardIds["Slot"], Is.EqualTo("reward-b"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t")]
        public void EmptySelectionIdsAreRejectedWithoutRecordingSelections(string invalidId)
        {
            var state = new MercenaryState(0);
            MercenaryInstance mercenary = state.AddMercenary("Name", MercenaryClass.Warrior, 10);

            Assert.Throws<ArgumentException>(() => mercenary.SelectSpecialization(invalidId));
            Assert.Throws<ArgumentException>(() => mercenary.SetSelectedReward(invalidId, "reward"));
            Assert.Throws<ArgumentException>(() => mercenary.SetSelectedReward("slot", invalidId));
            Assert.That(mercenary.SpecializationId, Is.Null);
            Assert.That(mercenary.SelectedRewardIds, Is.Empty);
        }

        [Test]
        public void RosterAndRewardViewsRejectCollectionMutation()
        {
            var state = new MercenaryState(0);
            MercenaryInstance mercenary = state.AddMercenary("Name", MercenaryClass.Rogue, 8);
            var roster = (IList<MercenaryInstance>)state.Mercenaries;
            var rewards = (IDictionary<string, string>)mercenary.SelectedRewardIds;

            Assert.Throws<NotSupportedException>(() => roster.Clear());
            Assert.Throws<NotSupportedException>(() => rewards.Add("slot", "reward"));
            Assert.That(state.Mercenaries, Has.Count.EqualTo(1));
            Assert.That(mercenary.SelectedRewardIds, Is.Empty);
        }
    }
}
