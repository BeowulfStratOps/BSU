using System;

namespace BSU.Core.Model
{
    public enum CalculatedRepositoryStateEnum
    {
        Loading, // At least one loading
        NeedsUpdate, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts. 
        NeedsDownload, // All mods are usable or need a download. TODO: combine download and update state?
        Ready, // All use
        RequiresUserIntervention, // Else
        InProgress // All are ready or being worked on
    }

    public class CalculatedRepositoryState : IEquatable<CalculatedRepositoryState>
    {
        public CalculatedRepositoryState(CalculatedRepositoryStateEnum state, bool isPartial)
        {
            State = state;
            IsPartial = isPartial;
        }

        public CalculatedRepositoryStateEnum State { get; }
        public bool IsPartial { get; }

        public override string ToString()
        {
            var res = State.ToString();
            if (IsPartial) res += " (partial)";
            return res;
        }

        public bool Equals(CalculatedRepositoryState other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return State == other.State && IsPartial == other.IsPartial;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CalculatedRepositoryState) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) State, IsPartial);
        }

        public static bool operator ==(CalculatedRepositoryState left, CalculatedRepositoryState right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CalculatedRepositoryState left, CalculatedRepositoryState right)
        {
            return !Equals(left, right);
        }
    }
}