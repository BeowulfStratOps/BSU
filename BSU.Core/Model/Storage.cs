using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model.Actions;
using BSU.Core.View;
using BSU.CoreCommon;
using StorageTarget = BSU.Core.Model.Actions.StorageTarget;

namespace BSU.Core.Model
{
    internal class Storage
    {
        public IStorage Implementation { get; }
        public string Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();
        public List<StorageMod> Mods { private set; get; } = new List<StorageMod>(); // TODO: readonly

        public JobSlot<SimpleJob> Loading { get; }
        
        internal Model Model { get; set; }

        public Storage(IStorage implementation, string identifier, string location)
        {
            Implementation = implementation;
            Identifier = identifier;
            Location = location;
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, $"Load Storage {Identifier}", 1));
            Loading.StartJob();
        }

        private void Load()
        {
            Implementation.Load();
            foreach (KeyValuePair<string,IStorageMod> mod in Implementation.GetMods())
            {
                var modelMod = new StorageMod(this, mod.Value, mod.Key);
                Mods.Add(modelMod);
                ModAdded?.Invoke(modelMod);
            }
        }

        public StorageMod CreateMod(string identifier, UpdateTarget updateTarget)
        {
            var mod = Implementation.CreateMod(identifier);
            var newMod = new StorageMod(this, mod, identifier, true);
            newMod.UpdateTarget = updateTarget;
            ModAdded?.Invoke(newMod);
            Model.MatchMaker.AddStorageMod(newMod);
            return newMod;
        }
        
        public event Action<StorageMod> ModAdded;
        
        public StorageTarget AsTarget => new StorageTarget(this);
    }
}