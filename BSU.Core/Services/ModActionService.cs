using System;
using BSU.Core.Model;
using BSU.CoreCommon.Hashes;

namespace BSU.Core.Services;

internal interface IModActionService
{
    ModActionEnum GetModAction(IModelRepositoryMod repoMod,
        IModelStorageMod storageMod);
}

internal class ModActionService : IModActionService
{
    public ModActionEnum GetModAction(IModelRepositoryMod repoMod,
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
                var version = HashHelper.CheckHash(HashType.Version, repoMod, storageMod);
                if (version == null) return ModActionEnum.Loading;
                if (version == true) return ModActionEnum.ContinueUpdate;
                var match = HashHelper.CheckHash(HashType.Match, repoMod, storageMod);
                if (match == null) return ModActionEnum.Loading;
                if (match == true) return ModActionEnum.AbortAndUpdate;
                return ModActionEnum.Unusable;
            }
            case StorageModStateEnum.Created:
            {
                var match = HashHelper.CheckHash(HashType.Match, repoMod, storageMod);
                if (match == null) return ModActionEnum.Loading;
                if (match == false)  return ModActionEnum.Unusable;
                var version = HashHelper.CheckHash(HashType.Version, repoMod, storageMod);
                if (version == null) return ModActionEnum.Loading;
                if (version == true) return ModActionEnum.Use;
                return storageMod.CanWrite ? ModActionEnum.Update : ModActionEnum.UnusableSteam;
            }
            case StorageModStateEnum.Updating:
            {
                var version = HashHelper.CheckHash(HashType.Version, repoMod, storageMod);
                if (version == null) return ModActionEnum.Loading;
                if (version == true) return ModActionEnum.Await;
                var match = HashHelper.CheckHash(HashType.Match, repoMod, storageMod);
                if (match == null) return ModActionEnum.Loading;
                if (match == true) return ModActionEnum.AbortActiveAndUpdate;
                return ModActionEnum.Unusable;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
