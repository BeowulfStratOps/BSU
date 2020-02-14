namespace BSU.Core.Model.Actions
{
    /// <summary>
    /// Representing the choice of updating a local mod to a different version (can be lower).
    /// </summary>
    internal class UpdateAction : ModAction
    {
        /// <summary>
        /// Update started previously.
        /// </summary>
        public bool IsContinuation { get; }
        
        public readonly StorageMod StorageMod;
        public readonly RepositoryMod RepositoryMod;

        internal UpdateAction(StorageMod storageMod, RepositoryMod repositoryMod, bool isContinuation,
            UpdateTarget updateTarget) : base(updateTarget)
        {
            IsContinuation = isContinuation;
            StorageMod = storageMod;
            RepositoryMod = repositoryMod;
        }

        public override string ToString()
        {
            var action = IsContinuation ? "Continue update" : "Update";
            return
                $"{action} {StorageMod.Storage.Identifier}/{StorageMod.Identifier} from \"{StorageMod.Implementation.GetDisplayName()}\"";
        }

        public StorageMod GetStorageMod() => StorageMod;
    }
}
