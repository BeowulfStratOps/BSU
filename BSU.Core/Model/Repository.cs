using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.JobManager;
using BSU.Core.View;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Repository
    {
        private readonly IJobManager _jobManager;
        private readonly MatchMaker _matchMaker;
        public IRepository Implementation { get; }
        public string Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();

        public List<RepositoryMod> Mods { get; } = new List<RepositoryMod>();
        
        public JobSlot<SimpleJob> Loading { get; }

        internal Model Model { get; set; }

        public Repository(IRepository implementation, string identifier, string location, IJobManager jobManager, MatchMaker matchMaker)
        {
            _jobManager = jobManager;
            _matchMaker = matchMaker;
            Location = location;
            Implementation = implementation;
            Identifier = identifier;
            var title = $"Load Repo {Identifier}";
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title, jobManager);
            Loading.StartJob();
        }

        private void Load()
        {
            Implementation.Load();
            foreach (KeyValuePair<string,IRepositoryMod> mod in Implementation.GetMods())
            {
                var modelMod = new RepositoryMod(this, mod.Value, mod.Key, _jobManager);
                Mods.Add(modelMod);
                ModAdded?.Invoke(modelMod);
                _matchMaker.AddRepositoryMod(modelMod);
            }
        }

        public event Action<RepositoryMod> ModAdded;
    }
}