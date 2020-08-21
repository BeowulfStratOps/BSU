using System.Collections.Generic;
using System.Linq;
using NLog;

namespace BSU.Core.Model
{
    internal class MatchMaker : IMatchMaker
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IModelStructure _modelStructure;
        private bool _allModsLoaded;

        public MatchMaker(IModelStructure modelStructure)
        {
            _modelStructure = modelStructure;
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
            foreach (var repoMod in _modelStructure.GetAllRepositoryMods())
            {
                CheckMatch(repoMod, storageMod);
            }
            CheckAllModsLoaded();
        }

        private void CheckAllModsLoaded()
        {
            if (_allModsLoaded) return;

            if (_modelStructure.GetStorages().Any(storage => storage.IsLoading)) return;

            if (_modelStructure.GetRepositories().Any(repository => repository.IsLoading)) return;

            if (_modelStructure.GetAllStorageMods().Any(storageMod => storageMod.GetState().State == StorageModStateEnum.Loading)) return;

            if (_modelStructure.GetAllRepositoryMods().Any(repoMod => repoMod.GetState().MatchHash == null)) return;

            _allModsLoaded = true;

            NotifyAllModsLoaded();

        }

        private void NotifyAllModsLoaded()
        {
            foreach (var repoMod in _modelStructure.GetAllRepositoryMods())
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
            foreach (var repoMod in _modelStructure.GetAllRepositoryMods())
            {
                repoMod.ChangeAction(mod, null);
            }
        }

        private void UpdateRepositoryMod(IModelRepositoryMod repositoryMod)
        {
            foreach (var storageMod in _modelStructure.GetAllStorageMods())
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

            if (match == CoreCalculation.ModMatch.RequireHash)
            {
                repoMod.ChangeAction(storageMod, ModActionEnum.Loading);
                storageMod.RequireHash();
                return;
            }

            if (match == CoreCalculation.ModMatch.NoMatch || match == CoreCalculation.ModMatch.Wait) return;

            var action = CoreCalculation.CalculateAction(repoModState, storageModState, storageMod.CanWrite);
            Logger.Debug($"Calculate Action on {repoMod} and {storageMod} -> {action}");

            repoMod.ChangeAction(storageMod, action);
        }
    }
}
