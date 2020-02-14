namespace BSU.Core.Model.Actions
{
    /// <summary>
    /// Represents the choice of waiting for a currently running job to finish.
    /// </summary>
    internal class AwaitUpdateAction : ModAction
    {
        public readonly StorageMod StorageMod;

        internal AwaitUpdateAction(StorageMod storageMod, UpdateTarget updateTarget) :
            base(updateTarget)
        {
            StorageMod = storageMod;
        }

        public override string ToString() =>
            $"Awaiting update of {StorageMod.Identifier} to {UpdateTarget.Hash} \"{UpdateTarget.Display}\"";

        public StorageMod GetStorageMod() => StorageMod;
    }
}
