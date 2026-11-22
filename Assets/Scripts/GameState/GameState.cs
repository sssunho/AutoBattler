using System;
using System.Collections.Generic;

namespace RaggedBanner.GameState
{
    public sealed class GameState
    {
        readonly HashSet<string> _tags;
        readonly List<ChronicleEntry> _chronicle;
        int _money;
        int _fame;
        int _reputation;
        RunPhase _runPhase;
        RunResult? _runResult;

        public MercenaryState MercenaryState { get; }
        public int Money => _money;
        public int Fame => _fame;
        public int Reputation => _reputation;
        public IReadOnlyCollection<string> Tags { get; }
        public RunPhase RunPhase => _runPhase;
        public RunResult? RunResult => _runResult;
        public ExplorationState ExplorationState { get; }
        public EventState EventState { get; }
        public BlessingState BlessingState { get; }
        public IReadOnlyList<ChronicleEntry> Chronicle { get; }

        public GameState(LocationKey startLocation, long initialExpeditionTime,
            int initialMoney, int initialFame, int initialReputation,
            IEnumerable<string> initialTags, int initialExperience,
            IEnumerable<string> regularEventIds,
            IReadOnlyDictionary<LocationKey, string[]> potentialEventIdsByLocation,
            IReadOnlyDictionary<string, string> initialArcStages,
            IEnumerable<string> initialRevealedGoalIds)
        {
            if (initialMoney < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialMoney));
            }

            if (initialFame < 0 || initialFame > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(initialFame));
            }

            if (initialReputation < -3 || initialReputation > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(initialReputation));
            }

            if (initialTags == null)
            {
                throw new ArgumentNullException(nameof(initialTags));
            }

            _tags = new HashSet<string>(StringComparer.Ordinal);
            foreach (string tagId in initialTags)
            {
                ValidateTagId(tagId);
                _tags.Add(tagId);
            }

            _money = initialMoney;
            _fame = initialFame;
            _reputation = initialReputation;
            _runPhase = RunPhase.Opening;
            _chronicle = new List<ChronicleEntry>();
            Tags = new ReadOnlySet<string>(_tags);
            Chronicle = _chronicle.AsReadOnly();
            MercenaryState = new MercenaryState(initialExperience);
            ExplorationState = new ExplorationState(startLocation, initialExpeditionTime);
            EventState = new EventState(regularEventIds, potentialEventIdsByLocation,
                initialArcStages, initialRevealedGoalIds);
            BlessingState = new BlessingState();
        }

        public void ApplyResourceChanges(int moneyChange, int fameChange, int reputationChange)
        {
            long money = (long)_money + moneyChange;
            if (money < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moneyChange), "Money cannot be negative.");
            }

            if (money > int.MaxValue)
            {
                throw new OverflowException("Money exceeds its storage range.");
            }

            int fame = (int)Math.Max(0L, Math.Min(10L, (long)_fame + fameChange));
            int reputation = (int)Math.Max(-3L, Math.Min(3L, (long)_reputation + reputationChange));
            _money = (int)money;
            _fame = fame;
            _reputation = reputation;
        }

        public bool HasTag(string tagId)
        {
            ValidateTagId(tagId);
            return _tags.Contains(tagId);
        }

        public bool AddTag(string tagId)
        {
            ValidateTagId(tagId);
            return _tags.Add(tagId);
        }

        public bool RemoveTag(string tagId)
        {
            ValidateTagId(tagId);
            return _tags.Remove(tagId);
        }

        public bool CompleteOpening()
        {
            if (_runPhase != RunPhase.Opening)
            {
                return false;
            }

            _runPhase = RunPhase.Normal;
            return true;
        }

        public bool TryEndRun(RunResult result)
        {
            result.Validate();
            if (_runPhase == RunPhase.Ended)
            {
                return false;
            }

            _runResult = result;
            _runPhase = RunPhase.Ended;
            return true;
        }

        public void AppendChronicle(ChronicleEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            _chronicle.Add(entry);
        }

        public RunSummary CreateRunSummary()
        {
            if (!_runResult.HasValue)
            {
                throw new InvalidOperationException("A run must end before its final summary is captured.");
            }

            return new RunSummary(this);
        }

        static void ValidateTagId(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId))
            {
                throw new ArgumentException("A tag ID is required.", nameof(tagId));
            }
        }
    }
}
