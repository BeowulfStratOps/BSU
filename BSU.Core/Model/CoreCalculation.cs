using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Persistence;

namespace BSU.Core.Model
{
    public static class CoreCalculation
    {
        internal static async Task<ModActionEnum?> GetModAction(MatchHash repoHash, VersionHash repoVersion,
            IModelStorageMod storageMod)
        {
            switch (storageMod.GetState())
            {
                case StorageModStateEnum.CreatedWithUpdateTarget:
                    if (repoVersion.IsMatch(await storageMod.GetVersionHash())) return ModActionEnum.ContinueUpdate;
                    return null;
                    if (repoHash.IsMatch(await storageMod.GetMatchHash())) return ModActionEnum.AbortAndUpdate; // TODO: not implemented!
                    return null;
                case StorageModStateEnum.Created:
                    if (!repoHash.IsMatch(await storageMod.GetMatchHash())) return null;
                    return repoVersion.IsMatch(await storageMod.GetVersionHash()) ? ModActionEnum.Use : ModActionEnum.Update;
                case StorageModStateEnum.Updating:
                    throw new NotImplementedException();
                case StorageModStateEnum.Error:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static RepositoryModActionSelection AutoSelect(bool allModsLoaded, Dictionary<IModelStorageMod, ModAction> actions,
            IModelStructure modelStructure, PersistedSelection usedMod)
        {
            if (usedMod != null)
            {
                if (usedMod.Mod == null && usedMod.Storage == null) return new RepositoryModActionSelection();

                var storage = modelStructure.GetStorages().FirstOrDefault(s => s.GetStorageIdentifier() == usedMod);
                if (storage != null) return new RepositoryModActionSelection(storage);

                var storageMod = actions.Keys.FirstOrDefault(mod => mod.GetStorageModIdentifiers() == usedMod);
                if (storageMod != null)
                {
                    return new RepositoryModActionSelection(storageMod);
                }
            }

            // Still loading
            if (!allModsLoaded) return null;
            if (actions.Values.Any(action => action.ActionType == ModActionEnum.Loading)) return null;

            // Order of precedence
            var precedence = new[]
                {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

            foreach (var actionType in precedence)
            {
                var storageMod = actions.Keys.FirstOrDefault(mod =>
                    actions[mod].ActionType == actionType && !actions[mod].Conflicts.Any());
                if (storageMod == null) continue;

                return new RepositoryModActionSelection(storageMod);
            }

            if (actions.All(am => am.Value.ActionType == ModActionEnum.Unusable))
            {
                var downloadStorage = modelStructure.GetStorages().FirstOrDefault(s => s.CanWrite);
                if (downloadStorage != null)
                {
                    return new RepositoryModActionSelection(downloadStorage);
                }
            }

            return null;
        }

        internal static CalculatedRepositoryState CalculateRepositoryState(List<IModelRepositoryMod> mods)
        {
            /*
            Loading, // 3. At least one loading
            NeedsUpdate, // 2. all selected, no internal conflicts.
            NeedsDownload, // 2. more than 50% of the mods need a download, otherwise same as update
            Ready, // 1. All use
            RequiresUserIntervention // Else
            */

            var partial = mods.Any(m => m.Selection?.DoNothing ?? false);

            mods = mods.Where(m => !(m.Selection?.DoNothing ?? false)).ToList();

            if (mods.All(mod =>
                mod.Selection?.StorageMod != null && mod.LocalMods[mod.Selection?.StorageMod].ActionType == ModActionEnum.Use))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Ready, partial);
            }

            if (mods.All(mod => mod.Selection?.StorageMod != null || mod.Selection?.DownloadStorage != null))
            {
                // No internal conflicts
                if (mods.Where(mod => mod.Selection?.StorageMod != null).All(mod =>
                    mod.LocalMods[mod.Selection?.StorageMod].Conflicts.All(conflict => !mods.Contains(conflict.Parent))))
                {
                    if (mods.Count(mod => mod.Selection?.DownloadStorage != null) > 0.5 * mods.Count)
                        return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsDownload, partial);
                    return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsUpdate, partial);
                }
            }

            if (mods.All(mod =>
                mod.Selection?.StorageMod == null && mod.Selection?.DownloadStorage == null &&
                mod.LocalMods.Any(kv => kv.Value.ActionType == ModActionEnum.Loading)))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading, partial);
            }

            return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.RequiresUserIntervention, partial);
        }
    }
}
