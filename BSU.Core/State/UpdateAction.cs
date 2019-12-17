namespace BSU.Core.State
{
    public class UpdateAction : ModAction, IHasLocalMod
    {
        public bool IsContinuation { get; }
        public readonly StorageMod LocalMod;
        public readonly RepoMod RemoteMod;

        internal UpdateAction(StorageMod localMod, RepoMod remoteMod, bool isContinuation, UpdateTarget updateTarget) : base(updateTarget)
        {
            IsContinuation = isContinuation;
            LocalMod = localMod;
            RemoteMod = remoteMod;
        }

        public override string ToString() => $"Update {LocalMod.Storage.Name}/{LocalMod.Name} from {LocalMod.VersionHash.GetHashString()} to {UpdateTarget.Hash} \"{UpdateTarget.Display}\"";
        public StorageMod GetLocalMod() => LocalMod;
    }
}