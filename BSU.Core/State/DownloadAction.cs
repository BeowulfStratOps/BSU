namespace BSU.Core.State
{
    public class DownloadAction : ModAction
    {
        public readonly Storage Storage;
        public string FolderName; // TODO: use it

        internal DownloadAction(Storage storage)
        {
            Storage = storage;
        }

        public override string ToString() => "Download to " + Storage.Location;
    }
}