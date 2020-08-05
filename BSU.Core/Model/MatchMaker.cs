using System;
using System.Collections.Generic;
using BSU.Core.Hashes;
using NLog;

namespace BSU.Core.Model
{
    internal class MatchMaker
    {
        // TODO: matchmaker should be fully synchronized and the main driving power of any stuff. mods can not change
        // state while the match maker isn't ready for them. (mod jobs are futures, they can't actually do anything)
        
        private readonly List<RepositoryMod> _repoMods = new List<RepositoryMod>();
        private readonly List<StorageMod> _storageMods = new List<StorageMod>();
        private readonly object _lock = new object(); // TODO: use some kind of re-entrant lock for less ugly state changed handler?

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private bool _allModsLoaded = false;

        // TODO: add check for started updates!

        public void AddStorageMod(StorageMod storageMod)
        {
            lock (_lock)
            {
                if (_storageMods.Contains(storageMod)) return; // TODO: update
                _storageMods.Add(storageMod);
                storageMod.StateChanged += () =>
                {
                    lock (_lock)
                    {
                        UpdateStorageMod(storageMod);
                    }
                };
                UpdateStorageMod(storageMod);
            }
        }

        private void UpdateStorageMod(StorageMod storageMod)
        {
            foreach (var repoMod in _repoMods)
            {
                CheckMatch(repoMod, storageMod);
            }
            CheckAllModsLoaded();
        }

        private void CheckAllModsLoaded()
        {
            if (_allModsLoaded) return;
            
            // TODO: check storages / repos
            
            foreach (var storageMod in _storageMods)
            {
                if (storageMod.GetState().MatchHash == null) return;
            }
            
            foreach (var repoMod in _repoMods)
            {
                if (repoMod.GetState().MatchHash == null) return;
            }

            foreach (var repoMod in _repoMods)
            {
                repoMod.NotifyAllModsLoaded();
            }

            _allModsLoaded = true;
        }

        public void AddRepositoryMod(RepositoryMod repoMod)
        {
            lock (_lock)
            {
                if (_repoMods.Contains(repoMod)) return; // TODO: update
                _repoMods.Add(repoMod);
                repoMod.StateChanged += () =>
                {
                    lock (_lock)
                    {
                        UpdateRepositoryMod(repoMod);
                    }
                };
                UpdateRepositoryMod(repoMod);
            }
        }

        public void RemoveStorageMod(StorageMod mod)
        {
            lock (_lock)
            {
                foreach (var repoMod in _repoMods)
                {
                    repoMod.ChangeAction(mod, null, _allModsLoaded);
                }
            }
        }

        private void UpdateRepositoryMod(RepositoryMod repositoryMod)
        {
            foreach (var storageMod in _storageMods)
            {
                CheckMatch(repositoryMod, storageMod);
            }
        }

        private void CheckMatch(RepositoryMod repoMod, StorageMod storageMod)
        {
            // TODO: is always called from locked context, but should do it again for explicity / safety
            
            var repoModState = repoMod.GetState();
            var storageModState = storageMod.GetState();

            var match = CoreCalculation.IsMatch(repoModState, storageModState);
            Logger.Debug($"Check Match on {repoMod.Identifier} and {storageMod.Identifier} -> {match}");

            if (match == CoreCalculation.ModMatch.RequireHash) storageMod.RequireHash();
            
            if (match == CoreCalculation.ModMatch.NoMatch || match == CoreCalculation.ModMatch.Wait) return;
            
            var action = CoreCalculation.CalculateAction(repoModState, storageModState, storageMod.Storage.Implementation.CanWrite());
            Logger.Debug($"Calculate Action on {repoMod.Identifier} and {storageMod.Identifier} -> {action}");
                
            repoMod.ChangeAction(storageMod, action, _allModsLoaded);
        }
    }
}
