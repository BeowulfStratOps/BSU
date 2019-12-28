using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.State
{
    /// <summary>
    /// Represents a repository mod in the context of a <see cref="BSU.Core.State.State"/>.
    /// </summary>
    public class RepositoryMod
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public readonly string Name;

        /// <summary>
        /// Available actions for this mod.
        /// </summary>
        public readonly IReadOnlyList<ModAction> Actions;

        /// <summary>
        /// Currently selected action for this mod. Must be on of the <see cref="Actions"/>
        /// </summary>
        public ModAction Selected; // TODO: setter: make sure it's in Actions

        /// <summary>
        /// Display name of this mod.
        /// </summary>
        public readonly string DisplayName;

        internal readonly Repository Repo;

        public readonly VersionHash VersionHash;

        internal readonly IRepositoryMod Mod;

        private readonly Uid _uid = new Uid();

        // TODO: find a better place for that
        internal RepositoryMod(IRepositoryMod mod, Repository repo)
        {
            Logger.Debug("Creating new state for repo mod {0} -> {1}", mod.GetUid(), _uid);

            Repo = repo;
            Mod = mod;

            DisplayName = mod.GetDisplayName();

            Name = mod.GetIdentifier();

            var matchHash = new MatchHash(mod);
            VersionHash = new VersionHash(mod);

            var actions = new List<ModAction>();

            var storageModMatches = repo.State.Storages.SelectMany(s => s.Mods)
                .Where(m => m.MatchHash.IsMatch(matchHash)).ToList();

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
                Logger.Debug("Checking local match {0}", storageMod.Uid);
                ModAction action = null;

                if (storageMod.ActiveJob == null)
                {
                    if (VersionHash.IsMatch(storageMod.VersionHash) && storageMod.UpdateTarget == null)
                        action = new UseAction(storageMod, target);
                    else
                    {
                        if (!storageMod.Storage.CanWrite) continue;
                        action = new UpdateAction(storageMod, this, startedUpdates.Contains(storageMod), target);
                    }
                }
                else
                {
                    if (storageMod.ActiveJob.Target.Hash == VersionHash.GetHashString())
                        action = new AwaitUpdateAction(storageMod, this, target);
                    else
                        continue;
                }

                Logger.Debug("Created action: {0}", action);

                actions.Add(action);
                storageMod.AddRelatedAction(action);
            }

            actions.AddRange(
                repo.State.Storages.Where(s => s.CanWrite).Select(s => new DownloadAction(s, this, target)));

            Actions = actions.AsReadOnly();

            if (actions.Any(a => a is UseAction))
            {
                Selected = actions[0];
                Logger.Debug("Auto-selecting {0}", Selected);
            }

            var continuation = actions.FirstOrDefault(a => a is UpdateAction update && update.IsContinuation);
            if (continuation != null)
            {
                Selected = continuation;
                Logger.Debug("Auto-selecting {0}", Selected);
            }
        }

        internal void CollectConflicts()
        {
            Logger.Debug("Collecting Conflicts in {0}", Name);
            foreach (var action in Actions)
            {
                Logger.Debug("Checking {0}", action);
                if (action is UseAction) continue;
                if (!(action is IHasStorageMod storageModAction)) continue;
                foreach (var other in storageModAction.GetStorageMod().GetRelatedActions())
                {
                    Logger.Debug("Checking against {0}", other);
                    if (action.UpdateTarget.Hash == other.UpdateTarget.Hash) continue;
                    action.AddConflict(other);
                    Logger.Debug("Added conflict {0} <-> {1}", action, other);
                }
            }
        }
    }
}
