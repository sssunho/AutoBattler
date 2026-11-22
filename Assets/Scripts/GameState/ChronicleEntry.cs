using System;

namespace RaggedBanner.GameState
{
    public sealed class ChronicleEntry
    {
        public string EventId { get; }
        public string ChoiceId { get; }
        public LocationKey Location { get; }
        public EventBattleOutcome? BattleOutcome { get; }
        public AppliedEventEffects AppliedEffects { get; }

        public ChronicleEntry(string eventId, string choiceId, LocationKey location,
            EventBattleOutcome? battleOutcome, AppliedEventEffects appliedEffects)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException("An event ID is required.", nameof(eventId));
            }

            if (string.IsNullOrWhiteSpace(choiceId))
            {
                throw new ArgumentException("A choice ID is required.", nameof(choiceId));
            }

            if (battleOutcome.HasValue && (uint)battleOutcome.Value > (uint)EventBattleOutcome.Defeat)
            {
                throw new ArgumentOutOfRangeException(nameof(battleOutcome));
            }

            if (appliedEffects == null)
            {
                throw new ArgumentNullException(nameof(appliedEffects));
            }

            if (battleOutcome == EventBattleOutcome.Defeat && !appliedEffects.IsEmpty)
            {
                throw new ArgumentException("A defeated event cannot apply its subsequent effects.", nameof(appliedEffects));
            }

            EventId = eventId;
            ChoiceId = choiceId;
            Location = location;
            BattleOutcome = battleOutcome;
            AppliedEffects = appliedEffects;
        }
    }
}
