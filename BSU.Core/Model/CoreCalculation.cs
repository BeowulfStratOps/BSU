using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Persistence;

namespace BSU.Core.Model
{
    public static class CoreCalculation
    {
        internal static ModActionEnum? GetModAction(IModelRepositoryMod repoMod,
            IModelStorageMod storageMod)
        {
            // TODO: RequireXHash should be called by caller. side effects are bad etc.

            switch (storageMod.GetState())
            {
                case StorageModStateEnum.Matched:
                    if (!repoMod.GetMatchHash().IsMatch(storageMod.GetMatchHash())) return null;
                    storageMod.RequireVersionHash();
                    return ModActionEnum.Loading;
                case StorageModStateEnum.LoadedWithUpdateTarget:
                    if (repoMod.GetVersionHash().IsMatch(storageMod.GetVersionHash())) return ModActionEnum.ContinueUpdate;
                    storageMod.RequireMatchHash();
                    return null;
                case StorageModStateEnum.MatchedWithUpdateTarget:
                    if (repoMod.GetVersionHash().IsMatch(storageMod.GetVersionHash())) return ModActionEnum.ContinueUpdate;
                    if (repoMod.GetMatchHash().IsMatch(storageMod.GetMatchHash())) return ModActionEnum.AbortAndUpdate;
                    return null;
                case StorageModStateEnum.Loaded:
                    storageMod.RequireMatchHash();
                    return ModActionEnum.LoadingMatch;
                case StorageModStateEnum.Updating:
                    if (repoMod.GetVersionHash().IsMatch(storageMod.GetVersionHash())) return ModActionEnum.Await;
                    if (repoMod.GetMatchHash().IsMatch(storageMod.GetMatchHash())) return ModActionEnum.AbortAndUpdate;
                    return null;
                case StorageModStateEnum.Error:
                    return null;
                case StorageModStateEnum.Versioned:
                    if (!repoMod.GetMatchHash().IsMatch(storageMod.GetMatchHash())) return null;
                    return repoMod.GetVersionHash().IsMatch(storageMod.GetVersionHash()) ? ModActionEnum.Use : ModActionEnum.Update;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static RepositoryModActionSelection AutoSelect(IModelRepositoryMod repoMod,
            IModelStructure modelStructure)
        {
            var storageMods = modelStructure.GetAllStorageMods().ToList();

            var actions = storageMods.ToDictionary(s => s, s => GetModAction(repoMod, s));

            storageMods = storageMods.Where(s => actions[s] != null).ToList();

            if (storageMods.Any(s => actions[s] == ModActionEnum.Loading || actions[s] == ModActionEnum.LoadingMatch))
                return null;

            var conflicts = storageMods.ToDictionary(s => s, s => GetConflicts(repoMod, s, modelStructure).Any());

            // Order of precedence
            var precedence = new[]
                {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

            foreach (var actionType in precedence)
            {
                var storageMod = actions.Keys.FirstOrDefault(mod =>
                    actions[mod] == actionType && !conflicts[mod]);
                if (storageMod == null) continue;

                return new RepositoryModActionSelection(storageMod);
            }

            if (actions.All(am => am.Value == ModActionEnum.Unusable))
            {
                var downloadStorage = modelStructure.GetStorages().FirstOrDefault(s => s.CanWrite);
                if (downloadStorage != null)
                {
                    return new RepositoryModActionSelection(downloadStorage);
                }
            }

            return null;
        }

        internal static List<IModelRepositoryMod> GetConflicts(IModelRepositoryMod origin, IModelStorageMod selected,
            IModelStructure structure)
        {
            var result = new List<IModelRepositoryMod>();
            foreach (var repositoryMod in structure.GetAllRepositoryMods())
            {
                if (repositoryMod == origin) continue;
                if (repositoryMod.GetVersionHash().IsMatch(origin.GetVersionHash())) continue; // can't possibly be a conflict

                if (selected.GetState() != StorageModStateEnum.Versioned && selected.GetState() != StorageModStateEnum.MatchedWithUpdateTarget) continue;

                // matches, but different target version -> conflict
                if (selected.GetMatchHash().IsMatch(repositoryMod.GetMatchHash())) result.Add(repositoryMod);
            }

            return result;
        }

        internal static CalculatedRepositoryState CalculateRepositoryState(List<IModelRepositoryMod> mods, IModelStructure structure)
        {
            /*
            Loading, // 3. At least one loading
            NeedsUpdate, // 2. all selected, no internal conflicts.
            NeedsDownload, // 2. more than 50% of the mods need a download, otherwise same as update
            Ready, // 1. All use
            RequiresUserIntervention // Else
            */

            if (mods.Any(m => !m.IsLoaded))
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading, false);

            var partial = mods.Any(m => m.Selection?.DoNothing ?? false);

            mods = mods.Where(m => !(m.Selection?.DoNothing ?? false)).ToList();

            if (mods.All(mod =>
                mod.Selection?.StorageMod != null && GetModAction(mod, mod.Selection?.StorageMod) == ModActionEnum.Use))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Ready, partial);
            }

            if (mods.All(mod => mod.Selection?.StorageMod != null || mod.Selection?.DownloadStorage != null))
            {

                if (mods.Count(mod => mod.Selection?.DownloadStorage != null) > 0.5 * mods.Count)
                    return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsDownload, partial);
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsUpdate, partial);
            }

            if (mods.All(mod =>
                mod.Selection?.StorageMod == null && mod.Selection?.DownloadStorage == null &&
                structure.GetAllStorageMods().Any(s => GetModAction(mod, s) == ModActionEnum.Loading || GetModAction(mod, s) == ModActionEnum.LoadingMatch)))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading, partial);
            }

            return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.RequiresUserIntervention, partial);
        }
    }
}
