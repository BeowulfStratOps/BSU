using System.Collections.Generic;
using System.Linq;
using BSU.CoreInterface;

namespace BSU.Core.State
{
    public class Storage
    {
        public readonly List<StorageMod> Mods;
        public readonly string Location;
        public readonly bool CanWrite;
        internal readonly State State;
        public readonly string Name;

        internal Storage(IStorage storage, State state)
        {
            Name = storage.GetIdentifier();
            State = state;
            Mods = storage.GetMods().Select(m => new StorageMod(m, this)).ToList();
            Location = storage.GetLocation();
            CanWrite = storage.CanWrite();
        }
    }
}