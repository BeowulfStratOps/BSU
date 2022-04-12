using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services
{
    internal static class AutoSelectorCalculation
    {
        public enum SteamUsage
        {
            DontUseSteam,
            UseSteamButPreferLocal,
            UseSteamAndPreferIt
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static bool HasInvalidSelection(IModelRepositoryMod mod, bool useSteam)
        {
            var currentSelection = mod.GetCurrentSelection();

            return currentSelection switch
            {
                ModSelectionNone => true,
                ModSelectionLoading => true,
                ModSelectionDownload download when download.DownloadStorage.IsDeleted => true,
                ModSelectionStorageMod storageMod when storageMod.StorageMod.IsDeleted ||
                                                       (!storageMod.StorageMod.CanWrite && !useSteam) => true,
                _ => false
            };
        }

        public static ModSelection? GetAutoSelection(IModel model, IModelRepositoryMod mod, SteamUsage steamUsage = SteamUsage.UseSteamButPreferLocal, bool allowSwitching = false)
        {
            var shouldDoSelection = allowSwitching || HasInvalidSelection(mod, steamUsage != SteamUsage.DontUseSteam);
            return shouldDoSelection ? GetSelection(model, mod, steamUsage) : null;
        }

        private static IModelStorageMod? AutoSelect(IModelRepositoryMod repoMod,
            IEnumerable<IModelStorageMod> storageMods, List<IModelRepositoryMod> allRepoMods, SteamUsage steamUsage)
        {
            var byActionType = new Dictionary<ModActionEnum, List<IModelStorageMod>>();

            foreach (var storageMod in storageMods)
            {
                if (CoreCalculation.GetConflictsUsingMod(repoMod, storageMod, allRepoMods).Any())
                    continue;
                var action = CoreCalculation.GetModAction(repoMod, storageMod);
                byActionType.AddInBin(action, storageMod);
            }

            // Order of precedence
            var precedence = new[]
                {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

            foreach (var actionType in precedence)
            {
                if (!byActionType.TryGetValue(actionType, out var candidates))
                    continue;

                var foundSteam = candidates.FirstOrDefault(mod => !mod.CanWrite);
                var foundNonSteam = candidates.FirstOrDefault(mod => mod.CanWrite);

                if (steamUsage == SteamUsage.DontUseSteam)
                {
                    if (foundNonSteam != null)
                        return foundNonSteam;
                    continue;
                }

                if (steamUsage == SteamUsage.UseSteamAndPreferIt)
                {
                    var preferred = foundSteam ?? foundNonSteam;
                    if (preferred != null)
                        return preferred;
                    continue;
                }

                if (steamUsage == SteamUsage.UseSteamButPreferLocal)
                {
                    var preferred = foundNonSteam ?? foundSteam;
                    if (preferred != null)
                        return preferred;
                    continue;
                }
            }

            return null;
        }

        private static ModSelection? GetSelection(IModel model, IModelRepositoryMod mod, SteamUsage steamUsage)
        {
            Logger.Trace($"Checking auto-selection for mod {mod.Identifier}");


            if (model.GetStorages().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();

            var storageMods = model.GetStorageMods().ToList();

            var previouslySelectedMod =
                storageMods.SingleOrDefault(m => m.GetStorageModIdentifiers().Equals(mod.GetPreviousSelection()));

            if (previouslySelectedMod != null)
                return new ModSelectionStorageMod(previouslySelectedMod);

            // TODO: check previously selected storage for download?

            // wait for everything to load.
            // TODO: only wait for things we really need here
            if (model.GetStorageMods().Any(s => s.GetState() == StorageModStateEnum.Loading)) return new ModSelectionLoading();
            if (model.GetRepositories().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();
            if (model.GetRepositoryMods().Any(s => s.State == LoadingState.Loading)) return new ModSelectionLoading();

            var selectedMod = AutoSelect(mod, storageMods, model.GetRepositoryMods(), steamUsage);

            if (selectedMod != null)
                return new ModSelectionStorageMod(selectedMod);

            var storage = model.GetStorages().FirstOrDefault(s => s.CanWrite && s.IsAvailable());
            if (storage != null)
            {
                var downloadName = CoreCalculation.GetAvailableDownloadIdentifier(storage, mod.Identifier);
                return new ModSelectionDownload(storage, downloadName);
            }

            return null;
        }
    }
}
