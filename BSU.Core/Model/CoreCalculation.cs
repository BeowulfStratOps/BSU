using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;

namespace BSU.Core.Model
{
    public static class CoreCalculation
    {
        // TODO: ideally, all that async stuff and calling other members in here should happen on the repo/repoMod. only reason it happens here is conflicts being annoying.

        internal static async Task<ModActionEnum> GetModAction(IModelRepositoryMod repoMod,
            IModelStorageMod storageMod, CancellationToken cancellationToken)
        {
            // TODO: handle errors

            // make sure that we abort if the state changes in between calls
            // TODO: might not be needed right now. probably needs some more investigation
            var stateToken = storageMod.GetStateToken();
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stateToken).Token;

            async Task<bool> CheckMatch()
            {
                var repoTask = repoMod.GetMatchHash(combinedToken);
                var storageTask = storageMod.GetMatchHash(combinedToken);
                return (await repoTask).IsMatch(await storageTask);
            }

            async Task<bool> CheckVersion()
            {
                var repoTask = repoMod.GetVersionHash(combinedToken);
                var storageTask = storageMod.GetVersionHash(combinedToken);
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
                    if (await CheckMatch()) return ModActionEnum.AbortActiveAndUpdate;
                    return ModActionEnum.Unusable;
                }
                case StorageModStateEnum.Error:
                    return ModActionEnum.Unusable;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static async Task<IModelStorageMod> AutoSelect(IModelRepositoryMod repoMod, IEnumerable<IModelStorageMod> storageMods, CancellationToken cancellationToken)
        {
            async Task<(IModelStorageMod mod, ModActionEnum action, bool hasConflcts)> GetModInfo(IModelStorageMod mod)
            {
                var actionTask = repoMod.GetActionForMod(mod, cancellationToken);
                var conflictTask = repoMod.GetConflictsUsingMod(mod, cancellationToken);
                await Task.WhenAll(actionTask, conflictTask);
                return (mod, actionTask.Result, conflictTask.Result.Any());
            }

            var infos = (await  storageMods.SelectAsync(GetModInfo)).ToList();

            // Order of precedence
            var precedence = new[]
                {ModActionEnum.Use, ModActionEnum.Await, ModActionEnum.ContinueUpdate, ModActionEnum.Update};

            foreach (var actionType in precedence)
            {
                var info = infos.FirstOrDefault(info => info.action == actionType && !info.hasConflcts);
                if (info == default) continue;

                return info.mod;
            }

            return null;
        }

        internal static async Task<bool> IsConflicting(IModelRepositoryMod origin, IModelRepositoryMod otherMod,
            IModelStorageMod selected, CancellationToken cancellationToken)
        {
            if (otherMod == origin) return false;
            var repoVersion = await otherMod.GetVersionHash(cancellationToken);
            if (repoVersion.IsMatch(await origin.GetVersionHash(cancellationToken)))
                return false; // can't possibly be a conflict

            // matches, but different target version -> conflict
            var repoMatch = await otherMod.GetMatchHash(cancellationToken);
            return repoMatch.IsMatch(await selected.GetMatchHash(cancellationToken));
        }

        internal static CalculatedRepositoryState CalculateRepositoryState(List<(RepositoryModActionSelection selection, ModActionEnum? action)> mods)
        {
            /*
            NeedsSync, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts.
            Ready, // All use
            RequiresUserIntervention, // Else
            Syncing, // All are ready or being worked on
            Loading
            */

            if (mods.All(mod =>
                mod.selection is RepositoryModActionStorageMod && mod.action == ModActionEnum.Use))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Ready);
            }

            if (mods.All(mod => mod.selection is RepositoryModActionDoNothing ||
                mod.selection is RepositoryModActionStorageMod && mod.action == ModActionEnum.Use))
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.ReadyPartial);
            }

            if (mods.Any(mod => mod.selection == null || mod.selection is RepositoryModActionStorageMod && mod.action == ModActionEnum.AbortActiveAndUpdate))
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.RequiresUserIntervention);

            if (mods.Any(mod => mod.action == ModActionEnum.Await))
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Syncing);

            return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.NeedsSync);
        }
    }

    internal enum AutoSelectResult
    {
        Success,
        Download,
        None
    }
}
