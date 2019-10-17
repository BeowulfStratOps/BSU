namespace BSU.Core.State
{
    public class UpdateAction : ModAction
    {
        public readonly StorageMod LocalMod;
        public readonly string VersionHash, VersionDisplay;

        internal UpdateAction(StorageMod localMod, RepoMod remoteMod)
        {
            LocalMod = localMod;
            VersionHash = remoteMod.VersionHash.GetHashString();
            VersionDisplay = remoteMod.DisplayName;
        }

        public override string ToString() => $"Update {LocalMod.Location} to {VersionHash} \"{VersionDisplay}\"";
    }
}