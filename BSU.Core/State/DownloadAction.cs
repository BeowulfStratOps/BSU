namespace BSU.Core.State
{
    public class DownloadAction : ModAction
    {
        public readonly Storage Storage;
        public readonly RepoMod RemoteMod;
        public string FolderName;

        internal DownloadAction(Storage storage, RepoMod remoteMod, UpdateTarget updateTarget) : base(updateTarget)
        {
            Storage = storage;
            RemoteMod = remoteMod;
        }

        public override string ToString() => "Download to " + Storage.Location;
    }
}