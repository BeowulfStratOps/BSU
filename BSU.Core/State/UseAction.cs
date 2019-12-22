namespace BSU.Core.State
{
    public class UseAction : ModAction, IHasStorageMod
    {
        public readonly StorageMod StorageMod;

        internal UseAction(StorageMod storageMod, UpdateTarget updateTarget) : base(updateTarget)
        {
            StorageMod = storageMod;
        }

        public override string ToString() => "Use " + StorageMod.Name;

        public StorageMod GetStorageMod() => StorageMod;
    }
}
