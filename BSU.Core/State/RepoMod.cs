using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.CoreCommon;

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

        // TODO: find a better place for that
        internal RepoMod(IRemoteMod mod, Repo repo)
        {
            Repo = repo;
            Mod = mod;

            DisplayName = mod.GetDisplayName();

            Name = mod.GetIdentifier();

            var matchHash = new MatchHash(mod);
            VersionHash = new VersionHash(mod);

            var actions = new List<ModAction>();

            var localMatches = repo.State.Storages.SelectMany(s => s.Mods).Where(m => m.MatchHash.IsMatch(matchHash)).ToList();

            var startedUpdates = repo.State.Storages.SelectMany(s => s.Mods)
                .Where(m => VersionHash.GetHashString().Equals(m.UpdateTarget?.Hash)).ToList();

            foreach (var startedUpdate in startedUpdates)
            {
                if (!localMatches.Contains(startedUpdate))
                    localMatches.Add(startedUpdate);
            }

            var target = new UpdateTarget(VersionHash.GetHashString(), DisplayName);

            foreach (var localMod in localMatches)
            {
                ModAction action;
                if (VersionHash.IsMatch(localMod.VersionHash) && localMod.UpdateTarget == null)
                    action = new UseAction(localMod, target);
                else
                {
                    if (!localMod.Storage.CanWrite) continue;
                    if (localMod.ActiveJob != null && localMod.ActiveJob.Target.Hash == VersionHash.GetHashString())
                        action = new AwaitUpdateAction(localMod, this, target);
                    else
                        action = new UpdateAction(localMod, this, startedUpdates.Contains(localMod), target);
                }

                actions.Add(action);
                localMod.AddRelatedAction(action);
            }

            actions.AddRange(repo.State.Storages.Where(s => s.CanWrite).Select(s => new DownloadAction(s, this, target)));

            Actions = actions.AsReadOnly();

            if (actions.Any(a => a is UseAction))
                Selected = actions[0];
        }

        internal void CollectConflicts()
        {
            foreach (var action in Actions)
            {
                if (action is UseAction) continue;
                if (!(action is IHasLocalMod localModAction)) continue;
                foreach (var other in localModAction.GetLocalMod().GetRelatedActions())
                {
                    if (action.UpdateTarget.Hash != other.UpdateTarget.Hash) action.AddConflict(other);
                }
            }
        }
    }
}
