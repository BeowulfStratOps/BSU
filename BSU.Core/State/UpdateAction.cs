namespace BSU.Core.State
{
    public class UpdateAction : ModAction
    {
        public bool IsContinuation { get; }
        public readonly StorageMod LocalMod;
        public readonly UpdateTarget Target;
        public readonly RepoMod RemoteMod;

        internal UpdateAction(StorageMod localMod, RepoMod remoteMod, bool IsContinuation)
        {
            this.IsContinuation = IsContinuation;
            LocalMod = localMod;
            RemoteMod = remoteMod;
            Target = new UpdateTarget(remoteMod.VersionHash.GetHashString(), remoteMod.DisplayName);
        }

        public override string ToString() => $"Update {LocalMod.Storage.Name}/{LocalMod.Name} from {LocalMod.VersionHash.GetHashString()} to {Target.Hash} \"{Target.Display}\"";
    }
}