namespace BSU.Core.State
{
    public class AwaitUpdateAction : ModAction
    {
        public readonly StorageMod LocalMod;
        public readonly RepoMod RemoteMod;
        public readonly UpdateTarget Target;

        internal AwaitUpdateAction(StorageMod localMod, RepoMod remoteMod)
        {
            LocalMod = localMod;
            Target = new UpdateTarget(remoteMod.VersionHash.GetHashString(), remoteMod.DisplayName);
            RemoteMod = remoteMod;
        }

        public override string ToString() => $"Awaiting update of {LocalMod.Name} to {Target.Hash} \"{Target.Display}\"";
    }
}