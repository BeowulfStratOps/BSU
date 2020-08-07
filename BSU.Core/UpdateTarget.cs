namespace BSU.Core
{
    /// <summary>
    /// Represents a mod version an update is aiming for.
    /// </summary>
    public class UpdateTarget
    {
        public readonly string Hash, Display;

        public UpdateTarget(string hash, string display)
        {
            Display = display;
            Hash = hash;
        }

        public override string ToString() => $"{Hash}:{Display}";
    }
}
