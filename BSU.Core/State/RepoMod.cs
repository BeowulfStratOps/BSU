using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.CoreInterface;

namespace BSU.Core.State
{
    public class RepoMod
    {
        public readonly string Name;
        public readonly IReadOnlyList<ModAction> Actions;
        public ModAction Selected = null;
        public readonly string DisplayName;

        internal readonly Repo Repo;

        public readonly VersionHash VersionHash;

        private readonly IRemoteMod _mod;

        internal RepoMod(IRemoteMod mod, Repo repo)
        {
            Repo = repo;
            _mod = mod;

            DisplayName = mod.GetDisplayName();

            Name = mod.GetIdentifier();

            var matchHash = new MatchHash(mod);
            VersionHash = new VersionHash(mod);

            var actions = new List<ModAction>();

            var localMatches = repo.State.Storages.SelectMany(s => s.Mods).Where(m => m._matchHash.IsMatch(matchHash));

            foreach (var localMod in localMatches)
            {
                if (VersionHash.Matches(localMod.VersionHash) && localMod.UpdateTarget == null)
                    actions.Add(new UseAction(localMod));
                else
                {
                    if (localMod.UpdateTarget != null && localMod.UpdateTarget.Hash == VersionHash.GetHashString())
                        actions.Add(new AwaitUpdateAction(localMod, this));
                    else
                        actions.Add(new UpdateAction(localMod, this));

                }
            }

            actions.AddRange(repo.State.Storages.Where(s => s.CanWrite).Select(s => new DownloadAction(s)));

            Actions = actions.AsReadOnly();
            if (actions.Any(a => a is UseAction))
                Selected = actions[0];
#if DEBUG
            Selected = actions[0];
            if (Name == "@ace")
            {
                Selected = actions.First(a => a is DownloadAction);
            }
#endif
        }
    }
}