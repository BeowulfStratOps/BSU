namespace BSU.Core.State
{
    public class DownloadAction : ModAction
    {
        public readonly Storage Storage;
        public readonly RepoMod RemoteMod;
        public readonly UpdateTarget Target;
        public string FolderName;

        internal DownloadAction(Storage storage, RepoMod remoteMod)
        {
            Storage = storage;
            RemoteMod = remoteMod;
            Target = new UpdateTarget(remoteMod.VersionHash.GetHashString(), remoteMod.DisplayName);
        }

        public override string ToString() => "Download to " + Storage.Location;
    }
}