using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Persistence;

namespace BSU.Core.Model
{
    public static class CoreCalculation
    {
        internal static ModActionEnum CalculateAction(RepositoryModState repoModState, StorageModState storageModState, bool storageWritable)
        {
            switch (storageModState.State)
            {
                case StorageModStateEnum.CreatedForDownload:
                    return ModActionEnum.Await;
                case StorageModStateEnum.Loading:
                    throw new InvalidCastException();
                case StorageModStateEnum.Loaded:
                case StorageModStateEnum.Hashing:
                    return ModActionEnum.Loading;
                case StorageModStateEnum.Hashed:
                    if (repoModState.VersionHash.IsMatch(storageModState.VersionHash)) return ModActionEnum.Use;
                    return storageWritable ? ModActionEnum.Update : ModActionEnum.Unusable;
                case StorageModStateEnum.Updating:
                    return storageModState.JobTarget.Hash == repoModState.VersionHash.GetHashString() ? ModActionEnum.Await : ModActionEnum.AbortAndUpdate;
                case StorageModStateEnum.CreatedWithUpdateTarget:
                    if (repoModState.VersionHash.GetHashString() != storageModState.UpdateTarget.Hash) throw new InvalidOperationException();
                    return ModActionEnum.ContinueUpdate;
                case StorageModStateEnum.ErrorUpdate:
                    return ModActionEnum.Error;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static ModMatch IsMatch(RepositoryModState repoModState, StorageModState storageModState)
        {
            if (repoModState.IsLoading) return ModMatch.Wait;
            if (repoModState.Error != null) return ModMatch.NoMatch;
            
            switch (storageModState.State)
            {
                case StorageModStateEnum.Loading:
                    return ModMatch.Wait;
                case StorageModStateEnum.Loaded:
                    return repoModState.MatchHash.IsMatch(storageModState.MatchHash)
                        ? ModMatch.RequireHash
                        : ModMatch.NoMatch; 
                case StorageModStateEnum.Hashing:
                    return ModMatch.Wait;
                case StorageModStateEnum.Hashed:
                    return repoModState.MatchHash.IsMatch(storageModState.MatchHash)
                        ? ModMatch.Match
                        : ModMatch.NoMatch;
                case StorageModStateEnum.Updating:
                case StorageModStateEnum.CreatedWithUpdateTarget:
                case StorageModStateEnum.CreatedForDownload:
                case StorageModStateEnum.ErrorUpdate:
                    return storageModState.UpdateTarget.Hash == repoModState.VersionHash.GetHashString()
                        ? ModMatch.Match
                        : ModMatch.NoMatch;
                case StorageModStateEnum.ErrorLoad:
                    return ModMatch.NoMatch;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal enum ModMatch
        {
            Wait,
            NoMatch,
            RequireHash,
            Match
        }

        internal static (IModelStorageMod, IModelStorage) AutoSelect(bool allModsLoaded, Dictionary<IModelStorageMod, ModAction> actions,
            IModelStructure modelStructure, StorageModIdentifiers usedMod)
        {
            if (usedMod != null)
            {
                var storageMod = actions.Keys.FirstOrDefault(mod => mod.GetStorageModIdentifiers() == usedMod);
                if (storageMod != null)
                {
                    return (storageMod, null);
                }
            }

            // Still loading
            if (!allModsLoaded) return (null, null);
            if (actions.Values.Any(action => action.ActionType == ModActionEnum.Loading)) return (null, null);

            // Order of precedence
            var precedence = new[]
                {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

            foreach (var actionType in precedence)
            {
                var storageMod = actions.Keys.FirstOrDefault(mod =>
                    actions[mod].ActionType == actionType && !actions[mod].Conflicts.Any());
                if (storageMod == null) continue;

                return (storageMod, null);
            }

            if (actions.All(am => am.Value.ActionType == ModActionEnum.Unusable))
            {
                var downloadStorage = modelStructure.GetStorages().FirstOrDefault(s => s.CanWrite);
                if (downloadStorage != null)
                {
                    return (null, downloadStorage);
                }
            }

            return (null, null);
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

            if (mods.All(mod =>
                mod.SelectedStorageMod != null && mod.Actions[mod.SelectedStorageMod].ActionType == ModActionEnum.Use))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Ready, false);
            }

            if (mods.All(mod => mod.SelectedStorageMod != null || mod.SelectedDownloadStorage != null))
            {
                // No internal conflicts
                if (mods.Where(mod => mod.SelectedStorageMod != null).All(mod =>
                    mod.Actions[mod.SelectedStorageMod].Conflicts.All(conflict => !mods.Contains(conflict.Parent))))
                {
                    if (mods.Count(mod => mod.SelectedDownloadStorage != null) > 0.5 * mods.Count)
                        return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsDownload, false);
                    return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsUpdate, false);
                }
            }

            if (mods.All(mod =>
                mod.SelectedStorageMod == null && mod.SelectedDownloadStorage == null &&
                mod.Actions.Any(kv => kv.Value.ActionType == ModActionEnum.Loading)))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading, false);
            }

            return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.RequiresUserIntervention, false);
        }
    }
}