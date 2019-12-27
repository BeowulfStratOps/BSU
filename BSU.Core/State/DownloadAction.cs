namespace BSU.Core.State
{
    public class DownloadAction : ModAction
    {
        public readonly Storage Storage;
        public readonly RepositoryMod RepositoryMod;
        public string FolderName;

        internal DownloadAction(Storage storage, RepositoryMod repositoryMod, UpdateTarget updateTarget) : base(
            updateTarget)
        {
            Storage = storage;
            RepositoryMod = repositoryMod;
        }

        public override string ToString() => "Download to " + Storage.Location;
    }
}