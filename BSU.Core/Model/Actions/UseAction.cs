namespace BSU.Core.Model.Actions
{
    /// <summary>
    /// Represent the choice of using a mod as is.
    /// </summary>
    internal class UseAction : ModAction
    {
        public readonly StorageMod StorageMod;

        internal UseAction(StorageMod storageMod, UpdateTarget updateTarget) : base(updateTarget)
        {
            StorageMod = storageMod;
        }

        public override string ToString() => "Use " + StorageMod.Identifier;

        public StorageMod GetStorageMod() => StorageMod;
    }
}
