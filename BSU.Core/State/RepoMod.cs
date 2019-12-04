﻿using System.Collections.Generic;
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

        internal readonly IRemoteMod Mod;

        internal RepoMod(IRemoteMod mod, Repo repo)
        {
            Repo = repo;
            Mod = mod;

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
            if (Selected == null) Selected = actions.FirstOrDefault(a => a is UpdateAction);
            if (Selected == null) Selected = actions.FirstOrDefault(a => a is AwaitUpdateAction);
#endif
        }

        public ISyncState PrepareUpdate(StorageMod local)
        {
            return Mod.PrepareSync(local.Mod);
        }
    }
}