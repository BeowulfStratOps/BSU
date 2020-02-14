using System.Collections.Generic;
using System.Linq;
using BSU.CoreCommon;

namespace BSU.Core.State
{
    /// <summary>
    /// Represents a storage in a <see cref="BSU.Core.State.State"/>
    /// </summary>
    public class Storage
    {
        public readonly List<StorageMod> Mods;
        public readonly string Location;
        public readonly bool CanWrite;
        internal readonly State State;
        public readonly string Name;
        internal readonly Model.Storage BackingStorage;

        internal readonly Uid Uid = new Uid();


        /// <summary>
        /// Remove this storage. Does not delete files. Invalidates the state.
        /// </summary>
        public void Remove()
        {
            State.InvalidateState();
        }

        internal Storage(Model.Storage storage, State state)
        {
            BackingStorage = storage;
            Name = storage.Identifier;
            State = state;
            Mods = storage.Mods.Select(m => new StorageMod(m, this)).ToList();
            Location = storage.Location;
            CanWrite = storage.Implementation.CanWrite();
        }
    }
}
