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
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, $"Load Storage {Identifier}", 1));
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

        public StorageMod CreateMod(string identifier, UpdateTarget updateTarget)
        {
            var mod = Implementation.CreateMod(identifier);
            var newMod = new StorageMod(this, mod, identifier, updateTarget);
            ModAdded?.Invoke(newMod);
            Model.MatchMaker.AddStorageMod(newMod);
            return newMod;
        }

        internal RepoSync PrepareDownload(RepositoryMod repositoryMod, string name)
        {
            // TODO: state lock? for this? for repo mod?
            var title = $"Downloading {repositoryMod.Implementation.GetDisplayName()} to {Location}";
            var target = new UpdateTarget(repositoryMod.GetState().VersionHash.GetHashString(), repositoryMod.Implementation.GetDisplayName());
            var storageMod = CreateMod(name, target);
            return new RepoSync(repositoryMod, storageMod, target, title, 0);
        }
        
        public event Action<StorageMod> ModAdded;
    }
}