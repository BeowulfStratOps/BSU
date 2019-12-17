namespace BSU.Core.State
{
    public class UseAction : ModAction, IHasLocalMod
    {
        public readonly StorageMod LocalMod;

        internal UseAction(StorageMod localMod, UpdateTarget updateTarget) : base(updateTarget)
        {
            LocalMod = localMod;
        }

        public override string ToString() => "Use " + LocalMod.Name;

        public StorageMod GetLocalMod() => LocalMod;
    }
}