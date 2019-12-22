namespace BSU.Core.State
{
    public class UpdateAction : ModAction, IHasStorageMod
    {
        public bool IsContinuation { get; }
        public readonly StorageMod StorageMod;
        public readonly RepositoryMod RepositoryMod;

        internal UpdateAction(StorageMod storageMod, RepositoryMod repositoryMod, bool isContinuation, UpdateTarget updateTarget) : base(updateTarget)
        {
            IsContinuation = isContinuation;
            StorageMod = storageMod;
            RepositoryMod = repositoryMod;
        }

        public override string ToString()
        {
            var action = IsContinuation ? "Continue update" : "Update";
            return $"{action} {StorageMod.Storage.Name}/{StorageMod.Name} from {StorageMod.VersionHash.GetHashString()} to {UpdateTarget.Hash} \"{UpdateTarget.Display}\"";
        }

        public StorageMod GetStorageMod() => StorageMod;
    }
}
