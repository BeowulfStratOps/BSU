namespace BSU.Core.State
{
    /// <summary>
    /// Represent the choice of using a mod as is.
    /// </summary>
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
