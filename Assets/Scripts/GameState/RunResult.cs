using System;

namespace RaggedBanner.GameState
{
    public readonly struct RunResult
    {
        public RunOutcome Outcome { get; }
        public string SucceededGoalId { get; }

        public RunResult(RunOutcome outcome, string succeededGoalId = null)
        {
            Outcome = outcome;
            SucceededGoalId = succeededGoalId;
            Validate();
        }

        internal void Validate()
        {
            if ((uint)Outcome > (uint)RunOutcome.TimeExpired)
            {
                throw new ArgumentOutOfRangeException(nameof(Outcome));
            }

            if (Outcome == RunOutcome.GoalSuccess)
            {
                if (string.IsNullOrWhiteSpace(SucceededGoalId))
                {
                    throw new ArgumentException("A successful run requires a goal ID.", nameof(SucceededGoalId));
                }
            }
            else if (SucceededGoalId != null)
            {
                throw new ArgumentException("Only a successful run can have a goal ID.", nameof(SucceededGoalId));
            }
        }
    }
}
