using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RaggedBanner.GameState;
using UnityEngine;

namespace RaggedBanner.Events.Tests
{
    public sealed class EventRuntimeTests
    {
        const int _areaId = 1;
        static readonly int[] _locationIds = { 1, 2, 3, 4, 5, 6, 7 };

        [Test]
        public void AuthoredCatalogLoadsAllSevenEventsWithoutBattleBindings()
        {
            EventCatalog catalog = Load();

            Assert.That(catalog.Events.Count, Is.EqualTo(7));
            Assert.That(catalog.Opening.Id, Is.EqualTo("opening_bottle_rumor"));
            Assert.That(catalog.Opening.IsAutomatic, Is.True);
            Assert.That(catalog.GetById("quarry_road_toll").Resolution.Choices[0].Conditions.MoneyMin, Is.EqualTo(10));
            Assert.That(catalog.GetById("quarry_road_toll").Resolution.Choices[1].Conditions.MoneyMin, Is.Null);
            Assert.That(catalog.GetById("giant_cabinet_keeper").Resolution.Choices[0].Result.Battle, Is.True);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)catalog.Opening.Conditions.Locations)[0] = 9);
        }

        [Test]
        public void NullTagConditionsMeanNoRestrictionAndZeroBoundsRemainExplicit()
        {
            Dictionary<string, string> sources = Sources();
            Edit(sources, "survey_wrong_scale", data =>
            {
                data["conditions"]["requiredTags"] = JValue.CreateNull();
                data["conditions"]["forbiddenTags"] = JValue.CreateNull();
                data["conditions"]["moneyMin"] = 0;
                data["conditions"]["moneyMax"] = 0;
                JToken choiceConditions = data["resolution"]["choices"][0]["conditions"];
                choiceConditions["requiredTags"] = JValue.CreateNull();
                choiceConditions["forbiddenTags"] = JValue.CreateNull();
                choiceConditions["moneyMin"] = 0;
            });
            EventCatalog catalog = Load(sources);
            EventDefinition definition = catalog.GetById("survey_wrong_scale");
            Assert.That(definition.Conditions.RequiredTags, Is.Empty);
            Assert.That(definition.Conditions.ForbiddenTags, Is.Empty);
            Assert.That(definition.Conditions.MoneyMax.HasValue, Is.True);
            Assert.That(definition.Conditions.MoneyMax.Value, Is.Zero);
            Assert.That(definition.Resolution.Choices[0].Conditions.MoneyMin.HasValue, Is.True);
            EventRunner runner = new EventRunner(catalog, catalog.CreateGameState(_areaId, 100, 0));
            CompleteOpening(runner);
            Assert.That(Enter(runner, 6), Is.True);
            Assert.That(runner.TryChoose("compare_plan"), Is.True);
        }

        [TestCase(0, 100L)]
        [TestCase(-1, 100L)]
        [TestCase(1, -1L)]
        public void InvalidInitialAreaOrTimeIsRejected(int areaId, long initialTime)
        {
            EventCatalog catalog = Load();
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.CreateGameState(areaId, initialTime, 0));
        }

        [Test]
        public void OpeningWaitsForInputAndCompletesOnlyOnce()
        {
            EventRunner runner = CreateRunner();

            Assert.That(runner.TryEnterLocation(Location(1)), Is.True);
            Assert.That(runner.State.ExplorationState.HasVisited(Location(1)), Is.True);
            Assert.That(runner.Stage, Is.EqualTo(EventRunnerStage.PresentingEvent));
            Assert.That(runner.State.RunPhase, Is.EqualTo(RunPhase.Opening));
            Assert.That(runner.State.Chronicle, Is.Empty);
            Assert.That(runner.TryChoose("wrong"), Is.False);
            Assert.That(runner.TryChoose("auto"), Is.True);
            Assert.That(runner.State.RunPhase, Is.EqualTo(RunPhase.Normal));
            Assert.That(runner.State.EventState.ResolvedEventCount, Is.EqualTo(1));
            Assert.That(runner.State.Chronicle[0].ChoiceId, Is.EqualTo("auto"));
            Assert.That(runner.State.Chronicle[0].BattleOutcome, Is.Null);
            Assert.That(runner.State.EventState.RemainingEventIds.Count, Is.EqualTo(6));
            Assert.That(runner.TryChoose("auto"), Is.False);
            Assert.That(runner.TryConfirmResult(), Is.True);
            Assert.That(runner.TryConfirmResult(), Is.False);
            Assert.That(runner.TryEnterLocation(Location(1)), Is.False);
            ResolveAt(runner, 3, "auto");
            Assert.That(Enter(runner, 1), Is.False);
            Assert.That(runner.State.EventState.ResolvedEventCount, Is.EqualTo(2));
        }

        [TestCase(0, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 2)]
        public void TollChoiceRequiresTenMoney(int money, int expectedChoices)
        {
            EventRunner runner = CreateRunner(money);
            CompleteOpening(runner);

            Assert.That(Enter(runner, 4), Is.True);
            Assert.That(runner.AvailableChoices.Count, Is.EqualTo(expectedChoices));
            Assert.That(HasChoice(runner, "pay_toll"), Is.EqualTo(money >= 10));
            Assert.That(runner.TryChoose("pay_toll"), Is.EqualTo(money >= 10));
            Assert.That(runner.State.Money, Is.EqualTo(money >= 10 ? money - 10 : money));
        }

        [Test]
        public void TollCannotBePaidTwiceOrChargedAgainOnRevisit()
        {
            EventRunner runner = CreateRunner(20);
            CompleteOpening(runner);
            Assert.That(Enter(runner, 4), Is.True);
            Assert.That(runner.TryChoose("pay_toll"), Is.True);
            Assert.That(runner.AppliedEffects.MoneyChange, Is.EqualTo(-10));
            Assert.That(runner.TryChoose("pay_toll"), Is.False);
            Assert.That(runner.TryChoose("drive_off_bandits"), Is.False);
            Assert.That(runner.TryConfirmResult(), Is.True);
            ResolveAt(runner, 3, "auto");

            Assert.That(Enter(runner, 4), Is.False);
            Assert.That(runner.State.Money, Is.EqualTo(10));
            Assert.That(runner.State.Chronicle.Count, Is.EqualTo(3));
            Assert.That(runner.State.EventState.IsRemaining("quarry_road_toll"), Is.False);
        }

        [Test]
        public void ChoiceConditionsAreRecheckedBeforeApplyingAnyEffects()
        {
            EventRunner runner = CreateRunner(10);
            CompleteOpening(runner);
            Assert.That(Enter(runner, 4), Is.True);
            runner.State.ApplyResourceChanges(-10, 0, 0);

            Assert.That(runner.TryChoose("pay_toll"), Is.False);
            Assert.That(runner.State.Money, Is.Zero);
            Assert.That(runner.State.EventState.IsRemaining("quarry_road_toll"), Is.True);
            Assert.That(runner.State.Chronicle.Count, Is.EqualTo(1));
            Assert.That(runner.AvailableChoices.Count, Is.EqualTo(1));
            Assert.That(runner.TryChoose("drive_off_bandits"), Is.True);
            Assert.That(runner.State.Fame, Is.EqualTo(1));
        }

        [TestCase("compare_plan", true, "service_ledge")]
        [TestCase("examine_glass", false, "front_assault")]
        public void SurveyChoiceControlsFinalApproachAndBattleAutomaticallySucceeds(
            string surveyChoice, bool knowsStructure, string finalChoice)
        {
            EventRunner runner = CreateRunner();
            CompleteOpening(runner);
            ResolveAt(runner, 6, surveyChoice);

            Assert.That(runner.State.HasTag("KnowsCliffStructure"), Is.EqualTo(knowsStructure));
            Assert.That(Enter(runner, 7), Is.True);
            Assert.That(HasChoice(runner, "service_ledge"), Is.EqualTo(knowsStructure));
            Assert.That(runner.TryChoose(finalChoice), Is.True);
            Assert.That(runner.State.HasTag("GiantApothecaryRecordsRecovered"), Is.True);
            Assert.That(runner.State.Chronicle[2].BattleOutcome, Is.EqualTo(EventBattleOutcome.Victory));
            Assert.That(runner.AppliedEffects.AddedTags, Is.EquivalentTo(new[] { "GiantApothecaryRecordsRecovered" }));
            Assert.That(runner.State.RunPhase, Is.EqualTo(RunPhase.Normal));
            Assert.That(runner.State.RunResult, Is.Null);
            Assert.That(runner.TryChoose(finalChoice), Is.False);
        }

        [Test]
        public void ChronicleRecordsClampedResourceDeltasAndOnlyChangedTags()
        {
            Dictionary<string, string> sources = Sources();
            Edit(sources, "survey_wrong_scale", data =>
            {
                JToken effects = data["resolution"]["choices"][0]["result"]["effects"];
                effects["fameChange"] = 10;
                effects["reputationChange"] = -10;
                effects["removeTags"] = new JArray("AlreadyAbsent");
            });
            EventRunner runner = CreateRunner(sources, initialFame: 9, initialReputation: -2);
            CompleteOpening(runner);
            runner.State.AddTag("KnowsCliffStructure");
            Assert.That(Enter(runner, 6), Is.True);
            Assert.That(runner.TryChoose("compare_plan"), Is.True);

            Assert.That(runner.AppliedEffects.FameChange, Is.EqualTo(1));
            Assert.That(runner.AppliedEffects.ReputationChange, Is.EqualTo(-1));
            Assert.That(runner.AppliedEffects.AddedTags, Is.Empty);
            Assert.That(runner.AppliedEffects.RemovedTags, Is.Empty);
            Assert.That(runner.State.Chronicle[1].AppliedEffects, Is.SameAs(runner.AppliedEffects));
            Assert.That(runner.State.Fame, Is.EqualTo(10));
            Assert.That(runner.State.Reputation, Is.EqualTo(-3));
        }

        [Test]
        public void NewlyEligibleEventRequiresActualReentryAndIsRecordedAfterSelection()
        {
            Dictionary<string, string> sources = Sources();
            Edit(sources, "outpost_old_emblem", data =>
                data["conditions"]["requiredTags"] = new JArray("KnowsCliffStructure"));
            EventRunner runner = CreateRunner(sources);
            CompleteOpening(runner);

            Assert.That(Enter(runner, 2), Is.False);
            Assert.That(runner.State.ExplorationState.HasSeenEligibleEvent(Location(2), "outpost_old_emblem"), Is.False);
            runner.State.AddTag("KnowsCliffStructure");
            Assert.That(runner.TryEnterLocation(Location(2)), Is.False);
            ResolveAt(runner, 3, "auto");
            Assert.That(Enter(runner, 2), Is.True);
            Assert.That(runner.State.ExplorationState.HasSeenEligibleEvent(Location(2), "outpost_old_emblem"), Is.True);
            Assert.That(runner.CurrentEvent.Id, Is.EqualTo("outpost_old_emblem"));
        }

        [Test]
        public void SharedEventIsConsumedGloballyAcrossItsLocations()
        {
            Dictionary<string, string> sources = Sources();
            sources.Remove("outpost_old_emblem");
            Edit(sources, "quarry_road_toll", data => data["conditions"]["locations"] = new JArray(2, 4));
            EventRunner runner = CreateRunner(sources);
            CompleteOpening(runner);
            ResolveAt(runner, 2, "drive_off_bandits");

            Assert.That(Enter(runner, 4), Is.False);
            Assert.That(runner.State.Fame, Is.EqualTo(1));
            Assert.That(runner.State.EventState.ResolvedEventCount, Is.EqualTo(2));
        }

        [TestCase("fameMin", 1)]
        [TestCase("fameMax", 0)]
        [TestCase("reputationMin", 1)]
        [TestCase("reputationMax", -1)]
        [TestCase("moneyMin", 1)]
        [TestCase("moneyMax", 0)]
        [TestCase("resolvedEventCountMin", 2)]
        [TestCase("resolvedEventCountMax", 0)]
        public void EventNumericConditionsFilterArrivalCandidates(string field, int bound)
        {
            Dictionary<string, string> sources = Sources();
            Edit(sources, "outpost_old_emblem", data => data["conditions"][field] = bound);
            EventRunner runner = CreateRunner(sources);
            CompleteOpening(runner);
            if (field == "fameMax")
            {
                runner.State.ApplyResourceChanges(0, 1, 0);
            }
            else if (field == "moneyMax")
            {
                runner.State.ApplyResourceChanges(1, 0, 0);
            }

            Assert.That(Enter(runner, 2), Is.False);
            Assert.That(runner.State.EventState.IsRemaining("outpost_old_emblem"), Is.True);
            Assert.That(runner.State.ExplorationState.HasSeenEligibleEvent(Location(2), "outpost_old_emblem"), Is.False);
        }

        [Test]
        public void ForbiddenTagsFilterBothEventsAndChoices()
        {
            Dictionary<string, string> sources = Sources();
            Edit(sources, "outpost_old_emblem", data => data["conditions"]["forbiddenTags"] = new JArray("Blocked"));
            Edit(sources, "survey_wrong_scale", data =>
                data["resolution"]["choices"][0]["conditions"]["forbiddenTags"] = new JArray("Blocked"));
            EventRunner runner = CreateRunner(sources);
            CompleteOpening(runner);
            runner.State.AddTag("Blocked");

            Assert.That(Enter(runner, 2), Is.False);
            Assert.That(Enter(runner, 6), Is.True);
            Assert.That(HasChoice(runner, "compare_plan"), Is.False);
            Assert.That(HasChoice(runner, "examine_glass"), Is.True);
        }

        [TestCase(0L)]
        [TestCase(-5L)]
        public void ExpiredTravelTimeDoesNotSkipArrivalEffectsOrEndTheRun(long time)
        {
            EventRunner runner = CreateRunner();
            CompleteOpening(runner);
            runner.State.ExplorationState.RemainingExpeditionTime = time;
            Assert.That(Enter(runner, 7), Is.True);
            Assert.That(runner.TryChoose("front_assault"), Is.True);
            Assert.That(runner.TryConfirmResult(), Is.True);

            Assert.That(runner.State.HasTag("GiantApothecaryRecordsRecovered"), Is.True);
            Assert.That(runner.State.ExplorationState.RemainingExpeditionTime, Is.EqualTo(time));
            Assert.That(runner.State.RunPhase, Is.EqualTo(RunPhase.Normal));
        }

        [Test]
        public void MoneyOverflowRejectsTheWholeResultWithoutApplyingTagsOrConsumingEvent()
        {
            Dictionary<string, string> sources = Sources();
            Edit(sources, "survey_wrong_scale", data =>
                data["resolution"]["choices"][0]["result"]["effects"]["moneyChange"] = 1);
            EventRunner runner = CreateRunner(sources, int.MaxValue);
            CompleteOpening(runner);
            Assert.That(Enter(runner, 6), Is.True);

            Assert.That(runner.TryChoose("compare_plan"), Is.False);
            Assert.That(runner.State.Money, Is.EqualTo(int.MaxValue));
            Assert.That(runner.State.HasTag("KnowsCliffStructure"), Is.False);
            Assert.That(runner.State.Chronicle.Count, Is.EqualTo(1));
            Assert.That(runner.State.EventState.IsRemaining("survey_wrong_scale"), Is.True);
            Assert.That(runner.Stage, Is.EqualTo(EventRunnerStage.PresentingEvent));
        }

        [Test]
        public void ForeignLocationsAndInvalidArrivalOrderDoNotRecordVisits()
        {
            EventRunner runner = CreateRunner();

            Assert.That(runner.TryEnterLocation(Location(2)), Is.False);
            Assert.That(runner.TryEnterLocation(new LocationKey(2, 1)), Is.False);
            runner.State.ExplorationState.CurrentLocation = Location(2);
            Assert.That(runner.TryEnterLocation(Location(2)), Is.False);
            Assert.That(runner.State.ExplorationState.VisitedLocations, Is.Empty);
        }

        [TestCase("missing")]
        [TestCase("unknown")]
        [TestCase("stringInteger")]
        [TestCase("floatInteger")]
        [TestCase("range")]
        [TestCase("unknownLocation")]
        [TestCase("duplicateLocation")]
        [TestCase("arc")]
        [TestCase("goal")]
        [TestCase("unsupportedType")]
        [TestCase("reveal")]
        [TestCase("transition")]
        [TestCase("battleType")]
        [TestCase("cost")]
        [TestCase("reservedChoice")]
        [TestCase("duplicateChoice")]
        [TestCase("conflictingTags")]
        [TestCase("noUnconditionalChoice")]
        public void MalformedOrUnsupportedContentFailsBeforeCreatingState(string mutation)
        {
            Dictionary<string, string> sources = Sources();
            Edit(sources, "quarry_road_toll", data => MutateInvalid(data, mutation));

            FormatException error = Assert.Throws<FormatException>(() => Load(sources));
            Assert.That(error.Message, Does.Contain("quarry_road_toll"));
        }

        [Test]
        public void MissingOpeningDuplicateEventAndCompetingNodeEventsAreRejected()
        {
            Dictionary<string, string> missing = Sources();
            missing.Remove("opening_bottle_rumor");
            Assert.Throws<FormatException>(() => Load(missing));

            Dictionary<string, string> duplicate = Sources();
            duplicate.Add("another-file", duplicate["quarry_road_toll"]);
            Assert.Throws<FormatException>(() => Load(duplicate));

            Dictionary<string, string> competing = Sources();
            Edit(competing, "quarry_road_toll", data => data["conditions"]["locations"] = new JArray(2));
            Assert.Throws<FormatException>(() => Load(competing));
        }

        [Test]
        public void InitialStateMustSatisfyOpeningAndExcludeRegularStartCandidates()
        {
            Dictionary<string, string> sources = Sources();
            Edit(sources, "opening_bottle_rumor", data => data["conditions"]["moneyMin"] = 1);
            EventCatalog catalog = Load(sources);
            Assert.Throws<InvalidOperationException>(() => catalog.CreateGameState(_areaId, 100, 0));

            sources = Sources();
            Edit(sources, "outpost_old_emblem", data => data["conditions"]["locations"] = new JArray(1));
            catalog = Load(sources);
            Assert.Throws<InvalidOperationException>(() => catalog.CreateGameState(_areaId, 100, 0));
        }

        static void MutateInvalid(JObject data, string mutation)
        {
            JToken conditions = data["conditions"];
            JToken choices = data["resolution"]["choices"];
            JToken effects = choices[0]["result"]["effects"];
            switch (mutation)
            {
                case "missing":
                    data.Remove("title");
                    break;
                case "unknown":
                    data["typo"] = true;
                    break;
                case "stringInteger":
                    conditions["moneyMin"] = "10";
                    break;
                case "floatInteger":
                    conditions["moneyMin"] = 10.5;
                    break;
                case "range":
                    conditions["fameMin"] = 4;
                    conditions["fameMax"] = 3;
                    break;
                case "unknownLocation":
                    conditions["locations"] = new JArray(99);
                    break;
                case "duplicateLocation":
                    conditions["locations"] = new JArray(4, 4);
                    break;
                case "arc":
                    data["arcId"] = "unsupported";
                    break;
                case "goal":
                    data["goalId"] = "unsupported";
                    break;
                case "unsupportedType":
                    data["eventType"] = "Goal";
                    break;
                case "reveal":
                    effects["revealGoalIds"] = new JArray("unsupported");
                    break;
                case "transition":
                    effects["arcTransition"] = new JObject();
                    break;
                case "battleType":
                    choices[0]["result"]["battle"] = "false";
                    break;
                case "cost":
                    effects["moneyChange"] = -11;
                    break;
                case "reservedChoice":
                    choices[0]["id"] = "auto";
                    break;
                case "duplicateChoice":
                    choices[1]["id"] = "pay_toll";
                    break;
                case "conflictingTags":
                    conditions["requiredTags"] = new JArray("same");
                    conditions["forbiddenTags"] = new JArray("same");
                    break;
                case "noUnconditionalChoice":
                    choices[1]["conditions"]["moneyMin"] = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation));
            }
        }

        static EventCatalog Load(Dictionary<string, string> sources = null)
        {
            return EventCatalog.Load(sources ?? Sources(), _locationIds, 1);
        }

        static EventRunner CreateRunner(int initialMoney = 0)
        {
            return CreateRunner(Sources(), initialMoney);
        }

        static EventRunner CreateRunner(Dictionary<string, string> sources,
            int initialMoney = 0, int initialFame = 0, int initialReputation = 0)
        {
            EventCatalog catalog = Load(sources);
            return new EventRunner(catalog, catalog.CreateGameState(_areaId, 100, initialMoney, initialFame, initialReputation));
        }

        static Dictionary<string, string> Sources()
        {
            string directory = Path.Combine(Application.dataPath, "Scenario", "Events", "Prototype");
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                result.Add(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));
            }

            return result;
        }

        static void Edit(Dictionary<string, string> sources, string eventId, Action<JObject> edit)
        {
            JObject data = JObject.Parse(sources[eventId]);
            edit(data);
            sources[eventId] = data.ToString();
        }

        static LocationKey Location(int nodeId)
        {
            return new LocationKey(_areaId, nodeId);
        }

        static bool Enter(EventRunner runner, int nodeId)
        {
            runner.State.ExplorationState.CurrentLocation = Location(nodeId);
            return runner.TryEnterLocation(Location(nodeId));
        }

        static void CompleteOpening(EventRunner runner)
        {
            Assert.That(runner.TryEnterLocation(Location(1)), Is.True);
            Assert.That(runner.TryChoose("auto"), Is.True);
            Assert.That(runner.TryConfirmResult(), Is.True);
        }

        static void ResolveAt(EventRunner runner, int nodeId, string choiceId)
        {
            Assert.That(Enter(runner, nodeId), Is.True);
            Assert.That(runner.TryChoose(choiceId), Is.True);
            Assert.That(runner.TryConfirmResult(), Is.True);
        }

        static bool HasChoice(EventRunner runner, string choiceId)
        {
            for (int i = 0; i < runner.AvailableChoices.Count; i++)
            {
                if (runner.AvailableChoices[i].Id == choiceId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
