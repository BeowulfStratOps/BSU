namespace BSU.Core.Model.Actions
{
    /// <summary>
    /// Represents the choice of downloading a mod to a new location
    /// </summary>
    internal class DownloadAction : ModAction
    {
        public readonly Storage Storage;
        public readonly RepositoryMod RepositoryMod;
        public string FolderName;

        internal DownloadAction(Storage storage, RepositoryMod repositoryMod, UpdateTarget updateTarget) : base(
            updateTarget)
        {
            FolderName = repositoryMod.Identifier; // TODO
            Storage = storage;
            RepositoryMod = repositoryMod;
        }

        public override string ToString() => "Download to " + Storage.Location;
    }
}
