using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Model;

namespace BSU.Core.Services
{
    internal static class CoreCalculation
    {
        // TODO: ideally, all that async stuff and calling other members in here should happen on the repo/repoMod. only reason it happens here is conflicts being annoying.

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
                var conflicts = Helper.GetConflictsUsingMod(repoMod, mod, allRepoMods);
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


        internal static IEnumerable<IModelRepositoryMod> GetUsedBy(StorageMod mod)
        {
            throw new NotImplementedException();
        }
    }
}
