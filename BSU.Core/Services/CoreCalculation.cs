using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Model;

namespace BSU.Core.Services
{
    internal static class CoreCalculation
    {
        // TODO: create more tests. especially for loading/error handling
        // TODO: split up. need to figure out how tho..

        internal static ModActionEnum GetModAction(IModelRepositoryMod repoMod,
            IModelStorageMod storageMod)
        {
            if (repoMod.State == LoadingState.Loading || storageMod.GetState() == StorageModStateEnum.Loading)
                return ModActionEnum.Unusable;

            // TODO: handle errors

            bool CheckMatch()
            {
                var repoHash = repoMod.GetMatchHash();
                var storageHash = storageMod.GetMatchHash();
                return repoHash.IsMatch(storageHash);
            }

            bool CheckVersion()
            {
                var repoHash = repoMod.GetVersionHash();
                var storageHash = storageMod.GetVersionHash();
                return repoHash.IsMatch(storageHash);
            }

            switch (storageMod.GetState())
            {
                case StorageModStateEnum.CreatedWithUpdateTarget:
                {
                    if (CheckVersion()) return ModActionEnum.ContinueUpdate;
                    if (CheckMatch()) return ModActionEnum.AbortAndUpdate;
                    return ModActionEnum.Unusable;
                }
                case StorageModStateEnum.Created:
                {
                    if (!CheckMatch())  return ModActionEnum.Unusable;
                    if (CheckVersion()) return ModActionEnum.Use;
                    return storageMod.CanWrite ? ModActionEnum.Update : ModActionEnum.Unusable;
                }
                case StorageModStateEnum.Updating:
                {
                    if (CheckVersion()) return ModActionEnum.Await;
                    if (CheckMatch()) return ModActionEnum.AbortActiveAndUpdate;
                    return ModActionEnum.Unusable;
                }
                case StorageModStateEnum.Error:
                    return ModActionEnum.Unusable;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static IModelStorageMod AutoSelect(IModelRepositoryMod repoMod, IEnumerable<IModelStorageMod> storageMods, List<IModelRepositoryMod> allRepoMods)
        {
            (IModelStorageMod mod, ModActionEnum action, bool hasConflcts) GetModInfo(IModelStorageMod mod)
            {
                var action = GetModAction(repoMod, mod);
                var conflicts = GetConflictsUsingMod(repoMod, mod, allRepoMods);
                return (mod, action, conflicts.Any());
            }

            var infos = storageMods.Select(GetModInfo).ToList();

            // Order of precedence
            var precedence = new[]
                {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

            foreach (var actionType in precedence)
            {
                // no steam
                var foundInfo = infos.FirstOrDefault(info => info.action == actionType && !info.hasConflcts && info.mod.CanWrite);
                if (foundInfo != default) return foundInfo.mod;

                // steam
                foundInfo = infos.FirstOrDefault(info => info.action == actionType && !info.hasConflcts && !info.mod.CanWrite);
                if (foundInfo != default) return foundInfo.mod;
            }

            return null;
        }

        internal static bool IsConflicting(IModelRepositoryMod origin, IModelRepositoryMod otherMod,
            IModelStorageMod selected)
        {
            if (otherMod == origin) return false;
            if (origin.State == LoadingState.Loading || otherMod.State == LoadingState.Loading ||
                selected.GetState() == StorageModStateEnum.Loading) return false;
            var repoVersion = otherMod.GetVersionHash();
            if (repoVersion.IsMatch(origin.GetVersionHash()))
                return false; // can't possibly be a conflict

            // matches, but different target version -> conflict
            var repoMatch = otherMod.GetMatchHash();
            return repoMatch.IsMatch(selected.GetMatchHash());
        }

        internal static CalculatedRepositoryStateEnum CalculateRepositoryState(List<(RepositoryModActionSelection selection, ModActionEnum? action, bool hasError)> mods)
        {
            /*
            NeedsSync, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts.
            Ready, // All use
            RequiresUserIntervention, // Else
            Syncing, // All are ready or being worked on
            Loading
            */

            if (mods.Any(mod => mod.hasError))
            {
                return CalculatedRepositoryStateEnum.RequiresUserIntervention;
            }

            if (mods.All(mod =>
                mod.selection is RepositoryModActionStorageMod && mod.action == ModActionEnum.Use))
            {
                return CalculatedRepositoryStateEnum.Ready;
            }

            if (mods.All(mod => mod.selection is RepositoryModActionDoNothing ||
                mod.selection is RepositoryModActionStorageMod && mod.action == ModActionEnum.Use))
            {
                return CalculatedRepositoryStateEnum.ReadyPartial;
            }

            if (mods.Any(mod => mod.selection == null || mod.selection is RepositoryModActionStorageMod && mod.action == ModActionEnum.AbortActiveAndUpdate))
                return CalculatedRepositoryStateEnum.RequiresUserIntervention;

            if (mods.Any(mod => mod.action == ModActionEnum.Await))
                return CalculatedRepositoryStateEnum.Syncing;

            return CalculatedRepositoryStateEnum.NeedsSync;
        }

        // TODO: create tests
        public static CalculatedRepositoryStateEnum GetRepositoryState(IModelRepository repo, IEnumerable<IModelRepositoryMod> allRepositoryMods)
        {
            if (repo.State == LoadingState.Loading) return CalculatedRepositoryStateEnum.Loading;
            if (repo.State == LoadingState.Error) return CalculatedRepositoryStateEnum.Error;

            var mods = repo.GetMods();
            var allMods = allRepositoryMods.ToList();

            (RepositoryModActionSelection selection, ModActionEnum? action, bool hasError) GetModSelection(IModelRepositoryMod mod)
            {
                var selection = mod.GetCurrentSelection();
                var action = selection is not RepositoryModActionStorageMod actionStorageMod
                    ? null
                    : (ModActionEnum?)GetModAction(mod, actionStorageMod.StorageMod);
                var hasError = GetErrorForSelection(mod, allMods) != null;
                return (selection, action, hasError);
            }

            var infos = mods.Select(GetModSelection).ToList();

            return CalculateRepositoryState(infos);
        }

        public static string GetErrorForSelection(IModelRepositoryMod mod, IEnumerable<IModelRepositoryMod> allRepositoryMods)
        {
            var selection = mod.GetCurrentSelection();

            switch (selection)
            {
                case null:
                    return "Select an action";
                case RepositoryModActionDoNothing:
                    return null;
                case RepositoryModActionDownload when string.IsNullOrWhiteSpace(mod.DownloadIdentifier):
                    return "Name must be a valid folder name";
                case RepositoryModActionDownload when mod.DownloadIdentifier.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || mod.DownloadIdentifier.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0:
                    return "Invalid characters in name";
                case RepositoryModActionDownload selectStorage:
                {
                    if (selectStorage.DownloadStorage.State == LoadingState.Loading) return null;
                    var folderExists = selectStorage.DownloadStorage.HasMod(mod.DownloadIdentifier);
                    return folderExists ? "Name in use" : null;
                }
                case RepositoryModActionStorageMod selectMod when GetModAction(mod, selectMod.StorageMod) == ModActionEnum.AbortActiveAndUpdate:
                    return "This mod is currently being updated";
                case RepositoryModActionStorageMod selectMod:
                {
                    var conflicts = GetConflictsUsingMod(mod, selectMod.StorageMod, allRepositoryMods);
                    if (!conflicts.Any())
                        return null;

                    var conflictNames = conflicts.Select(c => $"{c}");
                    return "In conflict with: " + string.Join(", ", conflictNames);
                }
                default:
                    return null;
            }
        }

        public static List<IModelRepositoryMod> GetConflictsUsingMod(IModelRepositoryMod repoMod, IModelStorageMod storageMod, IEnumerable<IModelRepositoryMod> allRepoMods)
        {
            var result = new List<IModelRepositoryMod>();

            foreach (var mod in allRepoMods)
            {
                if (mod == repoMod) continue;
                if (mod.GetCurrentSelection() is not RepositoryModActionStorageMod otherMod || otherMod.StorageMod != storageMod) continue;
                if (IsConflicting(repoMod, mod, storageMod))
                    result.Add(mod);
            }

            return result;
        }

        public static string GetAvailableDownloadIdentifier(IModelStorage storage, string baseIdentifier)
        {
            bool Exists(string name)
            {
                return storage.GetMods().Any(
                    m => string.Equals(m.Identifier, name, StringComparison.InvariantCultureIgnoreCase));
            }

            if (!Exists(baseIdentifier))
                return baseIdentifier;
            var i = 1;
            while (true)
            {
                var name = $"{baseIdentifier}_{i}";
                if (!Exists(name))
                    return name;
                i++;
            }
        }

        public static IEnumerable<IModelRepositoryMod> GetUsedBy(IModelStorageMod storageMod, IEnumerable<IModelRepositoryMod> allRepositoryMods)
        {
            var result = new List<IModelRepositoryMod>();
            foreach (var repositoryMod in allRepositoryMods)
            {
                var selection = repositoryMod.GetCurrentSelection();
                if (selection is RepositoryModActionStorageMod mod && mod.StorageMod == storageMod)
                    result.Add(repositoryMod);
            }

            return result;
        }
    }
}
