using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.State
{
    public class Storage
    {
        public readonly List<StorageMod> Mods;
        public readonly string Location;
        public readonly bool CanWrite;
        internal readonly State State;
        public readonly string Name;
        internal readonly IStorage BackingStorage;

        internal readonly Uid Uid = new Uid();


        public void Remove()
        {
            State.Core.RemoveStorage(this);
            State.InvalidateState();
        }

        internal Storage(IStorage storage, State state)
        {
            BackingStorage = storage;
            Name = storage.GetIdentifier();
            State = state;
            Mods = storage.GetMods().Select(m => new StorageMod(m, this)).ToList();
            Location = storage.GetLocation();
            CanWrite = storage.CanWrite();
        }
    }
}
