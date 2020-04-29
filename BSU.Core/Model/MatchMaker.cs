using System;
using System.Collections.Generic;
using BSU.Core.Hashes;
using NLog;

namespace BSU.Core.Model
{
    internal class MatchMaker
    {
        private readonly List<RepositoryMod> _repoMods = new List<RepositoryMod>();
        private readonly List<StorageMod> _storageMods = new List<StorageMod>();
        private readonly object _lock = new object(); // TODO: use some kind of re-entrant lock for less ugly state changed handler?

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

            var (match, requireHash) = CoreCalculation.IsMatch(repoModState, storageModState);
            Logger.Debug($"Check Match on {repoMod.Identifier} and {storageMod.Identifier} -> {match}, {requireHash}");

            if (requireHash) storageMod.RequireHash();
            
            if (!match) return;
            
            var action = CoreCalculation.CalculateAction(repoModState, storageModState, storageMod.Storage.Implementation.CanWrite());
            Logger.Debug($"Calculate Action on {repoMod.Identifier} and {storageMod.Identifier} -> {action}");
                
            repoMod.ChangeAction(storageMod, action);
        }
    }
}
