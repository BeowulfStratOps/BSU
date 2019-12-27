namespace BSU.Core
{
    public class UpdateTarget
    {
        public readonly string Hash, Display;

        public UpdateTarget(string hash, string display)
        {
            Display = display;
            Hash = hash;
        }
    }
}