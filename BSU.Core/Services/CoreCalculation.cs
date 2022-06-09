using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.CoreCommon.Hashes;

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
                return ModActionEnum.Loading;

            if (repoMod.State == LoadingState.Error || storageMod.GetState() == StorageModStateEnum.Error)
                return ModActionEnum.Unusable;

            // TODO: there gotta be a less ugly way....
            
            switch (storageMod.GetState())
            {
                case StorageModStateEnum.CreatedWithUpdateTarget:
                {
                    var version = CheckHash(HashType.Version, repoMod, storageMod);
                    if (version == null) return ModActionEnum.Loading;
                    if (version == true) return ModActionEnum.ContinueUpdate;
                    var match = CheckHash(HashType.Match, repoMod, storageMod);
                    if (match == null) return ModActionEnum.Loading;
                    if (match == true) return ModActionEnum.AbortAndUpdate;
                    return ModActionEnum.Unusable;
                }
                case StorageModStateEnum.Created:
                {
                    var match = CheckHash(HashType.Match, repoMod, storageMod);
                    if (match == null) return ModActionEnum.Loading;
                    if (match == false)  return ModActionEnum.Unusable;
                    var version = CheckHash(HashType.Version, repoMod, storageMod);
                    if (version == null) return ModActionEnum.Loading;
                    if (version == true) return ModActionEnum.Use;
                    return storageMod.CanWrite ? ModActionEnum.Update : ModActionEnum.UnusableSteam;
                }
                case StorageModStateEnum.Updating:
                {
                    var version = CheckHash(HashType.Version, repoMod, storageMod);
                    if (version == null) return ModActionEnum.Loading;
                    if (version == true) return ModActionEnum.Await;
                    var match = CheckHash(HashType.Match, repoMod, storageMod);
                    if (match == null) return ModActionEnum.Loading;
                    if (match == true) return ModActionEnum.AbortActiveAndUpdate;
                    return ModActionEnum.Unusable;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool? CheckHash(HashType type, IHashCollection mod1, IHashCollection mod2)
        {
            var supportedTypes1 = mod1.GetSupportedHashTypes();
            var supportedTypes2 = mod2.GetSupportedHashTypes();
            var supportedType = supportedTypes1.Intersect(supportedTypes2).Where(t => HashHelper.GetHashType(t) == type)
                .MaxBy(HashHelper.GetPriority);

            if (supportedType == null)
                throw new InvalidOperationException();

            var hashTask1 = mod1.GetHash(supportedType);
            var hashTask2 = mod2.GetHash(supportedType);
            
            if (!hashTask1.IsCompleted || !hashTask2.IsCompleted) return null;

            var hash1 = hashTask1.GetAwaiter().GetResult();
            var hash2 = hashTask2.GetAwaiter().GetResult();

            return hash1.IsMatch(hash2);
        }

        private static bool IsConflicting(IModelRepositoryMod origin, IModelRepositoryMod otherMod,
            IModelStorageMod selected)
        {
            if (origin.State == LoadingState.Loading || otherMod.State == LoadingState.Loading ||
                selected.GetState() == StorageModStateEnum.Loading) return false;

            if (CheckHash(HashType.Version, otherMod, origin) != false)
                return false; // that's fine, won't break anything

            if (CheckHash(HashType.Match, otherMod, selected) != true)
                return false; // unrelated mod or loading, we don't care

            var actionType = GetModAction(origin, selected);

            if (actionType == ModActionEnum.Use) return false; // not our problem. only show conflict when we're trying to change something

            return true;
        }

        // TODO: create tests
        public static CalculatedRepositoryStateEnum GetRepositoryState(IModelRepository repo, IEnumerable<IModelRepositoryMod> allRepositoryMods)
        {
            /*
            NeedsSync, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts.
            Ready, // All use
            RequiresUserIntervention, // Else
            Syncing, // All are ready or being worked on
            Loading
            */

            if (repo.State == LoadingState.Loading) return CalculatedRepositoryStateEnum.Loading;
            if (repo.State == LoadingState.Error) return CalculatedRepositoryStateEnum.Error;

            var mods = repo.GetMods();
            var allMods = allRepositoryMods.ToList();

            var infos = new List<(ModSelection? selection, ModActionEnum? action)>();

            foreach (var mod in mods)
            {
                var selection = mod.GetCurrentSelection();
                var action = selection is not ModSelectionStorageMod actionStorageMod
                    ? null
                    : (ModActionEnum?)GetModAction(mod, actionStorageMod.StorageMod);

                var error = GetErrorForSelection(mod, allMods);
                if  (error != null)
                    return CalculatedRepositoryStateEnum.RequiresUserIntervention;

                if (action == ModActionEnum.Loading || selection is ModSelectionLoading)
                    return CalculatedRepositoryStateEnum.Loading;

                infos.Add((selection, action));
            }

            if (infos.All(mod =>
                    mod.selection is ModSelectionStorageMod && mod.action == ModActionEnum.Use))
            {
                return CalculatedRepositoryStateEnum.Ready;
            }

            if (infos.All(mod => mod.selection is ModSelectionDisabled))
            {
                return CalculatedRepositoryStateEnum.RequiresUserIntervention;
            }

            if (infos.All(mod => mod.selection is ModSelectionDisabled || mod.action == ModActionEnum.Use))
            {
                return CalculatedRepositoryStateEnum.ReadyPartial;
            }

            if (infos.Any(mod => mod.selection is ModSelectionNone || mod.action == ModActionEnum.AbortActiveAndUpdate))
                return CalculatedRepositoryStateEnum.RequiresUserIntervention;

            if (infos.Any(mod => mod.action == ModActionEnum.Await))
                return CalculatedRepositoryStateEnum.Syncing;

            return CalculatedRepositoryStateEnum.NeedsSync;
        }

        public static string? GetErrorForSelection(IModelRepositoryMod mod, IEnumerable<IModelRepositoryMod> allRepositoryMods)
        {
            var selection = mod.GetCurrentSelection();

            switch (selection)
            {
                case ModSelectionDisabled:
                    return null;
                case ModSelectionDownload download when !download.IsNameValid(out var error):
                {
                    return error;
                }
                case ModSelectionDownload selectStorage:
                {
                    if (selectStorage.DownloadStorage.State == LoadingState.Loading) return null;
                    var folderExists = selectStorage.DownloadStorage.HasMod(selectStorage.DownloadName);
                    return folderExists ? "Name in use" : null;
                }
                case ModSelectionStorageMod selectMod when GetModAction(mod, selectMod.StorageMod) == ModActionEnum.AbortActiveAndUpdate:
                    return "This mod is currently being updated";
                case ModSelectionStorageMod selectMod:
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
                if (mod.GetCurrentSelection() is not ModSelectionStorageMod otherMod || otherMod.StorageMod != storageMod) continue;
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
                if (selection is ModSelectionStorageMod mod && mod.StorageMod == storageMod)
                    result.Add(repositoryMod);
            }

            return result;
        }
    }
}
