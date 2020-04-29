using System;
using System.Collections.Generic;
using BSU.Core.Sync;
using BSU.Core.View;
using BSU.CoreCommon;

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
            var title = $"Load Storage {Identifier}";
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title);
            Loading.StartJob();
        }

        private void Load()
        {
            Implementation.Load();
            foreach (KeyValuePair<string,IStorageMod> mod in Implementation.GetMods())
            {
                var modelMod = new StorageMod(this, mod.Value, mod.Key, null);
                Mods.Add(modelMod);
                ModAdded?.Invoke(modelMod);
                Model.MatchMaker.AddStorageMod(modelMod);
            }
        }

        internal RepoSync StartDownload(RepositoryMod repositoryMod, string identifier)
        {
            // TODO: state lock? for this? for repo mod?
            var updateTarget = new UpdateTarget(repositoryMod.GetState().VersionHash.GetHashString(), repositoryMod.Implementation.GetDisplayName());
            var mod = Implementation.CreateMod(identifier);
            var storageMod = new StorageMod(this, mod, identifier, updateTarget);
            var sync = storageMod.StartUpdate(repositoryMod);
            ModAdded?.Invoke(storageMod);
            Model.MatchMaker.AddStorageMod(storageMod);
            return sync;
        }
        
        public event Action<StorageMod> ModAdded;
    }
}