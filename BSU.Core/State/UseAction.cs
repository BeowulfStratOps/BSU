namespace BSU.Core.State
{
    public class UseAction : ModAction
    {
        public readonly StorageMod LocalMod;

        internal UseAction(StorageMod localMod)
        {
            LocalMod = localMod;
        }

        public override string ToString() => "Use " + LocalMod.Location;
    }
}