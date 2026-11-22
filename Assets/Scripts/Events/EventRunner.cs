using System;
using System.Collections.Generic;
using RaggedBanner.GameState;
using ExpeditionState = RaggedBanner.GameState.GameState;

namespace RaggedBanner.Events
{
    public enum EventRunnerStage
    {
        Idle,
        PresentingEvent,
        ApplyingResult,
        PresentingResult
    }

    public sealed class EventRunner
    {
        readonly EventCatalog _catalog;
        readonly int _areaId;
        readonly List<EventChoice> _availableChoices;
        LocationKey? _lastEnteredLocation;
        EventRunnerStage _stage;
        EventDefinition _currentEvent;
        EventResult _currentResult;
        AppliedEventEffects _appliedEffects = AppliedEventEffects.Empty;

        public ExpeditionState State { get; }
        public EventRunnerStage Stage => _stage;
        public EventDefinition CurrentEvent => _currentEvent;
        public IReadOnlyList<EventChoice> AvailableChoices { get; }
        public EventResult CurrentResult => _currentResult;
        public AppliedEventEffects AppliedEffects => _appliedEffects;

        public EventRunner(EventCatalog catalog, ExpeditionState state)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            State = state ?? throw new ArgumentNullException(nameof(state));
            _areaId = state.ExplorationState.CurrentLocation.AreaId;
            _availableChoices = new List<EventChoice>();
            AvailableChoices = _availableChoices.AsReadOnly();
            if (!catalog.ContainsLocation(state.ExplorationState.CurrentLocation.NodeId))
            {
                throw new ArgumentException("The current location is absent from the event catalog.", nameof(state));
            }
        }

        public bool TryEnterLocation(LocationKey location)
        {
            if (Stage != EventRunnerStage.Idle || State.RunPhase == RunPhase.Ended
                || location != State.ExplorationState.CurrentLocation || location.AreaId != _areaId
                || !_catalog.ContainsLocation(location.NodeId)
                || (_lastEnteredLocation.HasValue && _lastEnteredLocation.Value == location))
            {
                return false;
            }

            if (State.RunPhase == RunPhase.Opening && location.NodeId != _catalog.StartLocationId)
            {
                return false;
            }

            _lastEnteredLocation = location;
            bool firstVisit = State.ExplorationState.RecordVisit(location);
            if (State.RunPhase == RunPhase.Opening)
            {
                if (!firstVisit || !_catalog.Opening.Conditions.Matches(State, location.NodeId))
                {
                    throw new InvalidOperationException("The Opening must be eligible on the first visit to the start location.");
                }

                Present(_catalog.Opening);
                return true;
            }

            IReadOnlyList<string> potentialIds = State.EventState.GetPotentialEventIds(location);
            var eligibleIds = new List<string>();
            EventDefinition selected = null;
            for (int i = 0; i < potentialIds.Count; i++)
            {
                string id = potentialIds[i];
                EventDefinition definition = _catalog.GetById(id);
                if (!State.EventState.IsRemaining(id) || !definition.Conditions.Matches(State, location.NodeId))
                {
                    continue;
                }

                eligibleIds.Add(id);
                if (firstVisit || !State.ExplorationState.HasSeenEligibleEvent(location, id))
                {
                    if (selected != null)
                    {
                        throw new InvalidOperationException("This prototype cannot select between multiple regular events at a location.");
                    }

                    selected = definition;
                }
            }

            State.ExplorationState.RecordEligibleEvents(location, eligibleIds);
            if (selected == null)
            {
                return false;
            }

            Present(selected);
            return true;
        }

        public bool TryChoose(string choiceId)
        {
            if (Stage != EventRunnerStage.PresentingEvent || State.RunPhase == RunPhase.Ended
                || !_lastEnteredLocation.HasValue
                || State.ExplorationState.CurrentLocation != _lastEnteredLocation.Value)
            {
                return false;
            }

            if (CurrentEvent.EventType == EventType.Normal && !State.EventState.IsRemaining(CurrentEvent.Id))
            {
                return false;
            }

            EventResult result = null;
            if (CurrentEvent.IsAutomatic)
            {
                if (choiceId != "auto")
                {
                    return false;
                }

                result = CurrentEvent.Resolution.Result;
            }
            else
            {
                RefreshChoices();
                for (int i = 0; i < _availableChoices.Count; i++)
                {
                    if (string.Equals(_availableChoices[i].Id, choiceId, StringComparison.Ordinal))
                    {
                        result = _availableChoices[i].Result;
                        break;
                    }
                }

                if (result == null)
                {
                    return false;
                }
            }

            long money = (long)State.Money + result.Effects.MoneyChange;
            if (money < 0 || money > int.MaxValue || State.EventState.ResolvedEventCount == int.MaxValue)
            {
                return false;
            }

            _stage = EventRunnerStage.ApplyingResult;
            EventBattleOutcome? battleOutcome = result.Battle ? EventBattleOutcome.Victory : (EventBattleOutcome?)null;
            int beforeMoney = State.Money;
            int beforeFame = State.Fame;
            int beforeReputation = State.Reputation;
            State.ApplyResourceChanges(result.Effects.MoneyChange, result.Effects.FameChange, result.Effects.ReputationChange);
            var added = new List<string>();
            var removed = new List<string>();
            for (int i = 0; i < result.Effects.AddTags.Count; i++)
            {
                string tag = result.Effects.AddTags[i];
                if (State.AddTag(tag))
                {
                    added.Add(tag);
                }
            }

            for (int i = 0; i < result.Effects.RemoveTags.Count; i++)
            {
                string tag = result.Effects.RemoveTags[i];
                if (State.RemoveTag(tag))
                {
                    removed.Add(tag);
                }
            }

            _appliedEffects = new AppliedEventEffects(added, removed, State.Fame - beforeFame,
                State.Reputation - beforeReputation, State.Money - beforeMoney, Array.Empty<string>(), null);
            if (CurrentEvent.EventType == EventType.Normal)
            {
                State.EventState.RecordRegularEventCompletion(CurrentEvent.Id);
            }
            else
            {
                State.EventState.RecordSpecialEventCompletion();
            }

            State.AppendChronicle(new ChronicleEntry(CurrentEvent.Id, choiceId, _lastEnteredLocation.Value,
                battleOutcome, AppliedEffects));
            if (CurrentEvent.EventType == EventType.Opening)
            {
                State.CompleteOpening();
            }

            _currentResult = result;
            _availableChoices.Clear();
            _stage = EventRunnerStage.PresentingResult;
            return true;
        }

        public bool TryConfirmResult()
        {
            if (Stage != EventRunnerStage.PresentingResult)
            {
                return false;
            }

            _currentEvent = null;
            _currentResult = null;
            _appliedEffects = AppliedEventEffects.Empty;
            _stage = EventRunnerStage.Idle;
            return true;
        }

        void Present(EventDefinition definition)
        {
            _currentEvent = definition;
            _currentResult = null;
            _appliedEffects = AppliedEventEffects.Empty;
            RefreshChoices();
            if (!definition.IsAutomatic && _availableChoices.Count == 0)
            {
                throw new InvalidOperationException("The current event has no available choice.");
            }

            _stage = EventRunnerStage.PresentingEvent;
        }

        void RefreshChoices()
        {
            _availableChoices.Clear();
            for (int i = 0; i < CurrentEvent.Resolution.Choices.Count; i++)
            {
                EventChoice choice = CurrentEvent.Resolution.Choices[i];
                if (choice.Conditions.Matches(State))
                {
                    _availableChoices.Add(choice);
                }
            }
        }
    }
}
