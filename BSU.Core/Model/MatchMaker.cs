using System.Collections.Generic;
using System.Linq;
using NLog;

namespace BSU.Core.Model
{
    internal class MatchMaker : IMatchMaker
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Model _model;
        private bool _allModsLoaded;

        public MatchMaker(Model model)
        {
            _model = model;
        }

        private IEnumerable<IModelStorageMod> GetAllStorageMods()
        {
            return _model.Storages.SelectMany(storage => storage.Mods);
        }
        
        private IEnumerable<IModelRepositoryMod> GetAllRepositoryMods()
        {
            return _model.Repositories.SelectMany(repository => repository.Mods);
        }

        public void AddStorageMod(IModelStorageMod storageMod)
        {
            _allModsLoaded = false;
            NotifyAllModsLoaded();
            storageMod.StateChanged += () =>
            {
                UpdateStorageMod(storageMod);
            };
            UpdateStorageMod(storageMod);
        }

        private void UpdateStorageMod(IModelStorageMod storageMod)
        {
            foreach (var repoMod in GetAllRepositoryMods())
            {
                CheckMatch(repoMod, storageMod);
            }
            CheckAllModsLoaded();
        }

        private void CheckAllModsLoaded()
        {
            if (_allModsLoaded) return;
            
            if (_model.Storages.Any(storage => storage.Loading.IsActive())) return;

            if (_model.Repositories.Any(repository => repository.Loading.IsActive())) return;
            
            if (GetAllStorageMods().Any(storageMod => storageMod.GetState().MatchHash == null)) return;
            
            if (GetAllRepositoryMods().Any(repoMod => repoMod.GetState().MatchHash == null)) return;

            _allModsLoaded = true;
            
            NotifyAllModsLoaded();

        }

        private void NotifyAllModsLoaded()
        {
            foreach (var repoMod in GetAllRepositoryMods())
            {
                repoMod.AllModsLoaded = _allModsLoaded;
            }
        }

        public void AddRepositoryMod(IModelRepositoryMod repoMod)
        {
            repoMod.StateChanged += () =>
            {
                    UpdateRepositoryMod(repoMod);
            };
            UpdateRepositoryMod(repoMod);
        }

        public void RemoveStorageMod(IModelStorageMod mod)
        {
            foreach (var repoMod in GetAllRepositoryMods())
            {
                repoMod.ChangeAction(mod, null);
            }
        }

        private void UpdateRepositoryMod(IModelRepositoryMod repositoryMod)
        {
            foreach (var storageMod in GetAllStorageMods())
            {
                CheckMatch(repositoryMod, storageMod);
            }
            CheckAllModsLoaded();
        }

        private static void CheckMatch(IModelRepositoryMod repoMod, IModelStorageMod storageMod)
        {
            var repoModState = repoMod.GetState();
            var storageModState = storageMod.GetState();

            var match = CoreCalculation.IsMatch(repoModState, storageModState);
            Logger.Debug($"Check Match on {repoMod} and {storageMod} -> {match}");

            if (match == CoreCalculation.ModMatch.RequireHash) storageMod.RequireHash();
            
            if (match == CoreCalculation.ModMatch.NoMatch || match == CoreCalculation.ModMatch.Wait) return;
            
            var action = CoreCalculation.CalculateAction(repoModState, storageModState, storageMod.CanWrite());
            Logger.Debug($"Calculate Action on {repoMod} and {storageMod} -> {action}");
                
            repoMod.ChangeAction(storageMod, action);
        }
    }
}
