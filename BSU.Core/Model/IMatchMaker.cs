namespace BSU.Core.Model
{
    internal interface IMatchMaker
    {
        void AddStorageMod(StorageMod storageMod);
        void AddRepositoryMod(RepositoryMod repoMod);
        void RemoveStorageMod(StorageMod mod);
    }
}