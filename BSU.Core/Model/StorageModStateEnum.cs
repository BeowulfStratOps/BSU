namespace BSU.Core.Model
{
    internal enum StorageModStateEnum
    {
        CreatedWithUpdateTarget,
        CreatedForDownload,
        Loading,
        Loaded,
        Hashing,
        Hashed,
        Updating
    }
}