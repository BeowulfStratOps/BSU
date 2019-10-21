using System.Collections.Generic;
using System.Linq;
using BSU.CoreInterface;

namespace BSU.Core.State
{
    public class State
    {
        public readonly List<Repo> Repos;
        public readonly List<Storage> Storages;
        internal readonly Core _core;

        internal State(IReadOnlyList<IRepository> repos, IReadOnlyList<IStorage> storages, Core core)
        {
            _core = core;
            Storages = storages.Select(s => new Storage(s, this)).ToList();
            Repos = repos.Select(r => new Repo(r, this)).ToList();
        }
    }
}