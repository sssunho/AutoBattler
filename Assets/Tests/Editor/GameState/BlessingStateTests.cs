using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace RaggedBanner.GameState.Tests
{
    public sealed class BlessingStateTests
    {
        [Test]
        public void NewRunHasNoTiersOrUsedConditions()
        {
            var state = new BlessingState();

            Assert.That(state.TiersByGodId, Is.Empty);
            Assert.That(state.UsedConditionIds, Is.Empty);
            Assert.That(state.GetTier("god"), Is.Zero);
            Assert.That(state.IsConditionUsed("condition"), Is.False);
        }

        [Test]
        public void AcquisitionsAdvanceOneTierAndTrackGodsIndependently()
        {
            var state = new BlessingState();

            Assert.That(state.TryRecordAcquisition("first", "condition-1"), Is.True);
            Assert.That(state.GetTier("first"), Is.EqualTo(1));
            Assert.That(state.TryRecordAcquisition("first", "condition-2"), Is.True);
            Assert.That(state.TryRecordAcquisition("second", "condition-3"), Is.True);
            Assert.That(state.GetTier("first"), Is.EqualTo(2));
            Assert.That(state.GetTier("second"), Is.EqualTo(1));
            Assert.That(state.UsedConditionIds, Has.Count.EqualTo(3));
            Assert.That(state.IsConditionUsed("condition-2"), Is.True);
        }

        [Test]
        public void UsedConditionCannotAdvanceAnyGodAgain()
        {
            var state = new BlessingState();
            state.TryRecordAcquisition("first", "condition");

            Assert.That(state.TryRecordAcquisition("first", "condition"), Is.False);
            Assert.That(state.TryRecordAcquisition("second", "condition"), Is.False);
            Assert.That(state.GetTier("first"), Is.EqualTo(1));
            Assert.That(state.GetTier("second"), Is.Zero);
            Assert.That(state.UsedConditionIds, Has.Count.EqualTo(1));
        }

        [Test]
        public void GodAndConditionIdsUseOrdinalComparison()
        {
            var state = new BlessingState();

            Assert.That(state.TryRecordAcquisition("god", "condition"), Is.True);
            Assert.That(state.TryRecordAcquisition("God", "Condition"), Is.True);
            Assert.That(state.GetTier("god"), Is.EqualTo(1));
            Assert.That(state.GetTier("God"), Is.EqualTo(1));
            Assert.That(state.GetTier("GOD"), Is.Zero);
            Assert.That(state.IsConditionUsed("CONDITION"), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t")]
        public void InvalidIdsDoNotPartiallyRecordAcquisition(string invalidId)
        {
            var state = new BlessingState();

            Assert.Throws<ArgumentException>(() => state.GetTier(invalidId));
            Assert.Throws<ArgumentException>(() => state.IsConditionUsed(invalidId));
            Assert.Throws<ArgumentException>(() => state.TryRecordAcquisition(invalidId, "condition"));
            Assert.Throws<ArgumentException>(() => state.TryRecordAcquisition("god", invalidId));
            Assert.That(state.TiersByGodId, Is.Empty);
            Assert.That(state.UsedConditionIds, Is.Empty);
        }

        [Test]
        public void TierOverflowDoesNotConsumeConditionOrChangeTier()
        {
            var state = new BlessingState();
            FieldInfo tiersField = typeof(BlessingState).GetField("_tiersByGodId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tiersField, Is.Not.Null);
            var tiers = (Dictionary<string, int>)tiersField.GetValue(state);
            tiers.Add("god", int.MaxValue);

            Assert.Throws<OverflowException>(() => state.TryRecordAcquisition("god", "condition"));
            Assert.That(state.GetTier("god"), Is.EqualTo(int.MaxValue));
            Assert.That(state.IsConditionUsed("condition"), Is.False);
            Assert.That(state.UsedConditionIds, Is.Empty);
        }

        [Test]
        public void ViewsPreventMutationAndNewRunsDoNotShareState()
        {
            var first = new BlessingState();
            first.TryRecordAcquisition("god", "condition");
            var tiers = (IDictionary<string, int>)first.TiersByGodId;
            var second = new BlessingState();

            Assert.Throws<NotSupportedException>(() => tiers["god"] = 20);
            Assert.That(first.UsedConditionIds, Is.Not.InstanceOf<HashSet<string>>());
            Assert.That(first.UsedConditionIds, Is.Not.InstanceOf<ICollection<string>>());
            Assert.That(second.GetTier("god"), Is.Zero);
            Assert.That(second.IsConditionUsed("condition"), Is.False);
        }
    }
}
