namespace BSU.Core.State
{
    public class AwaitUpdateAction : ModAction, IHasLocalMod
    {
        public readonly StorageMod LocalMod;
        public readonly RepoMod RemoteMod;

        internal AwaitUpdateAction(StorageMod localMod, RepoMod remoteMod, UpdateTarget updateTarget) : base(updateTarget)
        {
            LocalMod = localMod;
            RemoteMod = remoteMod;
        }

        public override string ToString() => $"Awaiting update of {LocalMod.Name} to {UpdateTarget.Hash} \"{UpdateTarget.Display}\"";

        public StorageMod GetLocalMod() => LocalMod;
    }
}