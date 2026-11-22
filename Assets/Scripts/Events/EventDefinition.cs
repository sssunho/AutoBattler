using System;
using System.Collections.Generic;
using ExpeditionState = RaggedBanner.GameState.GameState;

namespace RaggedBanner.Events
{
    public enum EventType
    {
        Opening,
        Normal
    }

    public enum EventResolutionType
    {
        Choice,
        Automatic
    }

    public sealed class EventDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public EventType EventType { get; }
        public EventConditions Conditions { get; }
        public EventResolution Resolution { get; }
        public bool IsAutomatic => Resolution.Type == EventResolutionType.Automatic;

        internal EventDefinition(string id, string title, string description, EventType eventType,
            EventConditions conditions, EventResolution resolution)
        {
            Id = id;
            Title = title;
            Description = description;
            EventType = eventType;
            Conditions = conditions;
            Resolution = resolution;
        }
    }

    public sealed class EventConditions
    {
        public IReadOnlyList<string> RequiredTags { get; }
        public IReadOnlyList<string> ForbiddenTags { get; }
        public int? FameMin { get; }
        public int? FameMax { get; }
        public int? ReputationMin { get; }
        public int? ReputationMax { get; }
        public int? MoneyMin { get; }
        public int? MoneyMax { get; }
        public int? ResolvedEventCountMin { get; }
        public int? ResolvedEventCountMax { get; }
        public IReadOnlyList<int> Locations { get; }

        internal EventConditions(string[] requiredTags, string[] forbiddenTags,
            int? fameMin, int? fameMax, int? reputationMin, int? reputationMax,
            int? moneyMin, int? moneyMax, int? resolvedEventCountMin,
            int? resolvedEventCountMax, int[] locations)
        {
            RequiredTags = Array.AsReadOnly((string[])requiredTags.Clone());
            ForbiddenTags = Array.AsReadOnly((string[])forbiddenTags.Clone());
            FameMin = fameMin;
            FameMax = fameMax;
            ReputationMin = reputationMin;
            ReputationMax = reputationMax;
            MoneyMin = moneyMin;
            MoneyMax = moneyMax;
            ResolvedEventCountMin = resolvedEventCountMin;
            ResolvedEventCountMax = resolvedEventCountMax;
            Locations = Array.AsReadOnly((int[])locations.Clone());
        }

        internal bool Matches(ExpeditionState state, int locationId)
        {
            bool containsLocation = false;
            for (int i = 0; i < Locations.Count; i++)
            {
                if (Locations[i] == locationId)
                {
                    containsLocation = true;
                    break;
                }
            }

            return containsLocation && MatchesTags(state, RequiredTags, ForbiddenTags)
                && InRange(state.Fame, FameMin, FameMax)
                && InRange(state.Reputation, ReputationMin, ReputationMax)
                && InRange(state.Money, MoneyMin, MoneyMax)
                && InRange(state.EventState.ResolvedEventCount, ResolvedEventCountMin, ResolvedEventCountMax);
        }

        internal static bool MatchesTags(ExpeditionState state,
            IReadOnlyList<string> requiredTags, IReadOnlyList<string> forbiddenTags)
        {
            for (int i = 0; i < requiredTags.Count; i++)
            {
                if (!state.HasTag(requiredTags[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < forbiddenTags.Count; i++)
            {
                if (state.HasTag(forbiddenTags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool InRange(int value, int? minimum, int? maximum)
        {
            return (!minimum.HasValue || value >= minimum.Value)
                && (!maximum.HasValue || value <= maximum.Value);
        }
    }

    public sealed class EventChoiceConditions
    {
        public IReadOnlyList<string> RequiredTags { get; }
        public IReadOnlyList<string> ForbiddenTags { get; }
        public int? MoneyMin { get; }

        internal EventChoiceConditions(string[] requiredTags, string[] forbiddenTags, int? moneyMin)
        {
            RequiredTags = Array.AsReadOnly((string[])requiredTags.Clone());
            ForbiddenTags = Array.AsReadOnly((string[])forbiddenTags.Clone());
            MoneyMin = moneyMin;
        }

        internal bool Matches(ExpeditionState state)
        {
            return EventConditions.MatchesTags(state, RequiredTags, ForbiddenTags)
                && (!MoneyMin.HasValue || state.Money >= MoneyMin.Value);
        }
    }

    public sealed class EventResolution
    {
        public EventResolutionType Type { get; }
        public IReadOnlyList<EventChoice> Choices { get; }
        public EventResult Result { get; }

        internal EventResolution(EventResolutionType type, EventChoice[] choices, EventResult result)
        {
            Type = type;
            Choices = Array.AsReadOnly((EventChoice[])choices.Clone());
            Result = result;
        }
    }

    public sealed class EventChoice
    {
        public string Id { get; }
        public string Text { get; }
        public EventChoiceConditions Conditions { get; }
        public EventResult Result { get; }

        internal EventChoice(string id, string text, EventChoiceConditions conditions, EventResult result)
        {
            Id = id;
            Text = text;
            Conditions = conditions;
            Result = result;
        }
    }

    public sealed class EventResult
    {
        public string Text { get; }
        public bool Battle { get; }
        public EventEffects Effects { get; }

        internal EventResult(string text, bool battle, EventEffects effects)
        {
            Text = text;
            Battle = battle;
            Effects = effects;
        }
    }

    public sealed class EventEffects
    {
        public IReadOnlyList<string> AddTags { get; }
        public IReadOnlyList<string> RemoveTags { get; }
        public int FameChange { get; }
        public int ReputationChange { get; }
        public int MoneyChange { get; }

        internal EventEffects(string[] addTags, string[] removeTags,
            int fameChange, int reputationChange, int moneyChange)
        {
            AddTags = Array.AsReadOnly((string[])addTags.Clone());
            RemoveTags = Array.AsReadOnly((string[])removeTags.Clone());
            FameChange = fameChange;
            ReputationChange = reputationChange;
            MoneyChange = moneyChange;
        }
    }
}
