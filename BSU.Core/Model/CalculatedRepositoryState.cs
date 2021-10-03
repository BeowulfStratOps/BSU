using System;

namespace BSU.Core.Model
{
    public enum CalculatedRepositoryStateEnum
    {
        NeedsSync, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts.
        Ready, // All use
        RequiresUserIntervention, // Else
        Syncing, // All are ready or being worked on
        Loading,
        ReadyPartial,
        Error
    }

    public record CalculatedRepositoryState(CalculatedRepositoryStateEnum State)
    {
        public override string ToString() => State.ToString();
    }
}
