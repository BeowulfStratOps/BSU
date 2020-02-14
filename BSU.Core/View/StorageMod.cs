namespace BSU.Core.View
{
    public class StorageMod
    {
        internal Model.StorageMod ModelStorageMod { get; }
        
        public string Identifier { get; set; }

        public JobSlot Loading { get; }
        public JobSlot Hashing { get; }
        public JobSlot Updating { get; }
        
        internal StorageMod(Model.StorageMod mod, ViewModel viewModel)
        {
            Loading = new JobSlot(mod.Loading, nameof(Loading));
            Hashing = new JobSlot(mod.Hashing, nameof(Hashing));
            Updating = new JobSlot(mod.Updating, nameof(Updating));
            ModelStorageMod = mod;
            Identifier = mod.Identifier;
            viewModel.StorageTargets[mod.AsTarget] = AsTarget;
        }
        
        internal StorageTarget AsTarget => new StorageTarget(this);
    }
}