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
            Logger.Info("Update Storage Mod {0}", storageMod.Identifier);
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

            Logger.Info("All Mods loaded");
            
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
            Logger.Info("Update Repository Mod {0}", repositoryMod.Identifier);
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
            Logger.Debug($"Check Match on {repoMod.Identifier} and {storageMod.Identifier} -> {match}");

            if (match == CoreCalculation.ModMatch.RequireHash)
            {
                repoMod.ChangeAction(storageMod, ModActionEnum.Loading);
                storageMod.RequireHash();
                return;
            }

            if (match == CoreCalculation.ModMatch.NoMatch || match == CoreCalculation.ModMatch.Wait) return;

            var action = CoreCalculation.CalculateAction(repoModState, storageModState, storageMod.CanWrite);
            Logger.Debug($"Calculate Action on {repoMod.Identifier} and {storageMod.Identifier} -> {action}");

            repoMod.ChangeAction(storageMod, action);
        }
    }
}
