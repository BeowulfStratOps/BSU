using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.CoreCommon;

namespace BSU.Core.State
{
    public class RepositoryMod
    {
        public readonly string Name;
        public readonly IReadOnlyList<ModAction> Actions;
        public ModAction Selected = null;
        public readonly string DisplayName;

        internal readonly Repo Repo;

        public readonly VersionHash VersionHash;

        internal readonly IRepositoryMod Mod;

        // TODO: find a better place for that
        internal RepositoryMod(IRepositoryMod mod, Repo repo)
        {
            Repo = repo;
            Mod = mod;

            DisplayName = mod.GetDisplayName();

            Name = mod.GetIdentifier();

            var matchHash = new MatchHash(mod);
            VersionHash = new VersionHash(mod);

            var actions = new List<ModAction>();

            var storageModMatches = repo.State.Storages.SelectMany(s => s.Mods).Where(m => m.MatchHash.IsMatch(matchHash)).ToList();

            var startedUpdates = repo.State.Storages.SelectMany(s => s.Mods)
                .Where(m => VersionHash.GetHashString().Equals(m.UpdateTarget?.Hash)).ToList();

            foreach (var startedUpdate in startedUpdates)
            {
                if (!storageModMatches.Contains(startedUpdate))
                    storageModMatches.Add(startedUpdate);
            }

            var target = new UpdateTarget(VersionHash.GetHashString(), DisplayName);

            foreach (var storageMod in storageModMatches)
            {
                ModAction action;
                if (VersionHash.IsMatch(storageMod.VersionHash) && storageMod.UpdateTarget == null)
                    action = new UseAction(storageMod, target);
                else
                {
                    if (!storageMod.Storage.CanWrite) continue;
                    if (storageMod.ActiveJob != null && storageMod.ActiveJob.Target.Hash == VersionHash.GetHashString())
                        action = new AwaitUpdateAction(storageMod, this, target);
                    else
                        action = new UpdateAction(storageMod, this, startedUpdates.Contains(storageMod), target);
                }

                actions.Add(action);
                storageMod.AddRelatedAction(action);
            }

            actions.AddRange(repo.State.Storages.Where(s => s.CanWrite).Select(s => new DownloadAction(s, this, target)));

            Actions = actions.AsReadOnly();

            if (actions.Any(a => a is UseAction))
                Selected = actions[0];

            var continuation = actions.FirstOrDefault(a => a is UpdateAction update && update.IsContinuation);
            if (continuation != null)
                Selected = continuation;
        }

        internal void CollectConflicts()
        {
            foreach (var action in Actions)
            {
                if (action is UseAction) continue;
                if (!(action is IHasStorageMod storageModAction)) continue;
                foreach (var other in storageModAction.GetStorageMod().GetRelatedActions())
                {
                    if (action.UpdateTarget.Hash != other.UpdateTarget.Hash) action.AddConflict(other);
                }
            }
        }
    }
}
