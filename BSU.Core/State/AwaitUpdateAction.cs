namespace BSU.Core.State
{
    public class AwaitUpdateAction : ModAction, IHasStorageMod
    {
        public readonly StorageMod StorageMod;
        public readonly RepositoryMod RepositoryMod;

        internal AwaitUpdateAction(StorageMod storageMod, RepositoryMod repositoryMod, UpdateTarget updateTarget) : base(updateTarget)
        {
            StorageMod = storageMod;
            RepositoryMod = repositoryMod;
        }

        public override string ToString() => $"Awaiting update of {StorageMod.Name} to {UpdateTarget.Hash} \"{UpdateTarget.Display}\"";

        public StorageMod GetStorageMod() => StorageMod;
    }
}
