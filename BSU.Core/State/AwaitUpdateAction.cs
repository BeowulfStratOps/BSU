namespace BSU.Core.State
{
    public class AwaitUpdateAction : ModAction
    {
        public readonly StorageMod LocalMod;
        public readonly string VersionHash, VersionDisplay;

        internal AwaitUpdateAction(StorageMod localMod, RepoMod remoteMod)
        {
            LocalMod = localMod;
            VersionHash = remoteMod.VersionHash.GetHashString();
            VersionDisplay = remoteMod.DisplayName;
        }

        public override string ToString() => $"Awaiting update of {LocalMod.Location} to {VersionHash} \"{VersionDisplay}\"";
    }
}