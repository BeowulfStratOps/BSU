using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.View;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Repository
    {
        public IRepository Implementation { get; }
        public string Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();

        public List<RepositoryMod> Mods { get; } = new List<RepositoryMod>();
        
        public JobSlot<SimpleJob> Loading { get; }

        internal Model Model { get; set; }

        public Repository(IRepository implementation, string identifier, string location)
        {
            Location = location;
            Implementation = implementation;
            Identifier = identifier;
            var title = $"Load Repo {Identifier}";
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title);
            Loading.StartJob();
        }

        private void Load()
        {
            Implementation.Load();
            foreach (KeyValuePair<string,IRepositoryMod> mod in Implementation.GetMods())
            {
                var modelMod = new RepositoryMod(this, mod.Value, mod.Key);
                Mods.Add(modelMod);
                ModAdded?.Invoke(modelMod);
                Model.MatchMaker.AddRepositoryMod(modelMod);
            }
        }

        public event Action<RepositoryMod> ModAdded;
    }
}