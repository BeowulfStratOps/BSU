namespace BSU.Core.Model
{
    internal interface IMatchMaker
    {
        void AddStorageMod(IModelStorageMod storageMod);
        void AddRepositoryMod(IModelRepositoryMod repoMod);
        void RemoveStorageMod(IModelStorageMod mod);
    }
}