using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace BSU.Core.Model
{
    public static class CoreCalculation
    {
        // TODO: ideally, all that async stuff and calling other members in here should happen on the repo/repoMod. only reason it happens here is conflicts being annoying.

        internal static async Task<ModActionEnum> GetModAction(IModelRepositoryMod repoMod,
            IModelStorageMod storageMod, CancellationToken cancellationToken)
        {
            // TODO: lock to make sure we get valid state?

            async Task<bool> CheckMatch()
            {
                var repoTask = repoMod.GetMatchHash(cancellationToken);
                var storageTask = storageMod.GetMatchHash(cancellationToken);
                return (await repoTask).IsMatch(await storageTask);
            }

            async Task<bool> CheckVersion()
            {
                var repoTask = repoMod.GetVersionHash(cancellationToken);
                var storageTask = storageMod.GetVersionHash(cancellationToken);
                return (await repoTask).IsMatch(await storageTask);
            }

            switch (storageMod.GetState())
            {
                case StorageModStateEnum.CreatedWithUpdateTarget:
                {
                    if (await CheckVersion()) return ModActionEnum.ContinueUpdate;
                    if (await CheckMatch()) return ModActionEnum.AbortAndUpdate;
                    return ModActionEnum.Unusable;
                }
                case StorageModStateEnum.Created:
                {
                    if (!await CheckMatch())  return ModActionEnum.Unusable;
                    if (await CheckVersion()) return ModActionEnum.Use;
                    return ModActionEnum.Update;
                }
                case StorageModStateEnum.Updating:
                {
                    if (await CheckVersion()) return ModActionEnum.Await;
                    if (await CheckMatch()) return ModActionEnum.AbortAndUpdate;
                    return ModActionEnum.Unusable;
                }
                case StorageModStateEnum.Error:
                    return ModActionEnum.Unusable;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static async Task<(AutoSelectResult result, IModelStorageMod mod)> AutoSelect(IModelRepositoryMod repoMod, IModelStructure structure, CancellationToken cancellationToken)
        {
            var storageMods = (await structure.GetStorageMods()).ToList();

            async Task<(IModelStorageMod mod, ModActionEnum action, bool hasConflcts)> GetModInfo(IModelStorageMod mod)
            {
                var actionTask = GetModAction(repoMod, mod, cancellationToken);
                var conflictTask = GetConflicts(repoMod, mod, structure, cancellationToken);
                await Task.WhenAll(actionTask, conflictTask);
                return (mod, actionTask.Result, conflictTask.Result.Any());
            }

            var infoTasks = storageMods.Select(GetModInfo).ToList();
            await Task.WhenAll(infoTasks);
            var infos = infoTasks.Select(t => t.Result).ToList();

            // Order of precedence
            var precedence = new[]
                {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

            foreach (var actionType in precedence)
            {
                var info = infos.FirstOrDefault(info => info.action == actionType && !info.hasConflcts);
                if (info == default) continue;

                return (AutoSelectResult.Success, info.mod);
            }

            if (infos.All(info => info.action == ModActionEnum.Unusable)) return (AutoSelectResult.Download, null);
            return (AutoSelectResult.None, null);
        }

        internal static async Task<List<IModelRepositoryMod>> GetConflicts(IModelRepositoryMod origin, IModelStorageMod selected,
            IModelStructure structure, CancellationToken cancellationToken)
        {
            var result = new List<IModelRepositoryMod>();

            var modTasks = structure.GetRepositories().Select(r => r.GetMods()).ToList();
            await Task.WhenAll(modTasks);
            var mods = modTasks.SelectMany(t => t.Result);

            // TODO: could parallelize
            foreach (var repositoryMod in mods)
            {
                if (repositoryMod == origin) continue;
                var repoVersion = await repositoryMod.GetVersionHash(cancellationToken);
                if (repoVersion.IsMatch(await origin.GetVersionHash(cancellationToken))) continue; // can't possibly be a conflict

                // matches, but different target version -> conflict
                var repoMatch = await repositoryMod.GetMatchHash(cancellationToken);
                if (repoMatch.IsMatch(await selected.GetMatchHash(cancellationToken))) result.Add(repositoryMod);
            }

            return result;
        }

        internal static CalculatedRepositoryState CalculateRepositoryState(List<(IModelRepositoryMod mod, RepositoryModActionSelection selection, ModActionEnum? action)> mods)
        {
            /*
            NeedsUpdate, // 2. all selected, no internal conflicts.
            NeedsDownload, // 2. more than 50% of the mods need a download, otherwise same as update
            Ready, // 1. All use
            RequiresUserIntervention // Else
            */

            var partial = mods.Any(m => m.selection?.DoNothing ?? false);

            mods = mods.Where(m => !(m.selection?.DoNothing ?? false)).ToList();

            if (mods.All(mod =>
                mod.selection?.StorageMod != null && mod.action == ModActionEnum.Use))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Ready, partial);
            }

            if (mods.All(mod => mod.selection?.StorageMod != null || mod.selection?.DownloadStorage != null))
            {

                if (mods.Count(mod => mod.selection?.DownloadStorage != null) > 0.5 * mods.Count)
                    return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsDownload, partial);
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsUpdate, partial);
            }

            return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.RequiresUserIntervention, partial);
        }
    }

    internal enum AutoSelectResult
    {
        Success,
        Download,
        None
    }
}
