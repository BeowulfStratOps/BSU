using System;

namespace BSU.Core.Model
{
    internal enum CalculatedRepositoryStateEnum
    {
        Loading, // At least one loading
        NeedsUpdate, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts. 
        NeedsDownload, // All mods are usable or need a download
        Ready, // All use
        RequiresUserIntervention, // Else
        InProgress // All are ready or being worked on
    }

    internal class CalculatedRepositoryState
    {
        public CalculatedRepositoryState(CalculatedRepositoryStateEnum state, bool isPartial)
        {
            State = state;
            IsPartial = isPartial;
        }

        public CalculatedRepositoryStateEnum State { get; }
        public bool IsPartial { get; }
        
        public bool Equals(CalculatedRepositoryState other)
        {
            return State == other.State && IsPartial == other.IsPartial;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) State, IsPartial);
        }
    }
}