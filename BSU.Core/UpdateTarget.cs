namespace BSU.Core
{
    /// <summary>
    /// Represents a mod version an update is aiming for.
    /// </summary>
    public class UpdateTarget
    {
        public readonly string Hash;

        public UpdateTarget(string hash)
        {
            Hash = hash;
        }

        public override string ToString() => $"{Hash}";
    }
}
