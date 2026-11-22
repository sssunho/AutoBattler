using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RaggedBanner.GameState
{
    public sealed class RunSummary
    {
        public RunResult Result { get; }
        public LocationKey LastLocation { get; }
        public int Money { get; }
        public int Fame { get; }
        public int Reputation { get; }
        public IReadOnlyList<string> Tags { get; }
        public IReadOnlyDictionary<string, ArcState> ArcStates { get; }
        public IReadOnlyList<ChronicleEntry> Chronicle { get; }

        internal RunSummary(GameState state)
        {
            Result = state.RunResult.Value;
            LastLocation = state.ExplorationState.CurrentLocation;
            Money = state.Money;
            Fame = state.Fame;
            Reputation = state.Reputation;
            Tags = new List<string>(state.Tags).AsReadOnly();
            Dictionary<string, ArcState> arcs = new Dictionary<string, ArcState>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, ArcState> arc in state.EventState.ArcStates)
            {
                arcs.Add(arc.Key, arc.Value);
            }

            ArcStates = new ReadOnlyDictionary<string, ArcState>(arcs);
            Chronicle = new List<ChronicleEntry>(state.Chronicle).AsReadOnly();
        }
    }
}
