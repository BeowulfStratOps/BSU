using System;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    public class CoreCalculation
    {
        internal static ModAction CalculateAction(RepositoryModState repoModState, StorageModState storageModState, bool storageWritable)
        {
            switch (storageModState.State)
            {
                case StorageModStateEnum.CreatedForDownload:
                    return ModAction.Await;
                case StorageModStateEnum.Loading:
                    throw new InvalidCastException();
                case StorageModStateEnum.Loaded:
                case StorageModStateEnum.Hashing:
                    return ModAction.Loading;
                case StorageModStateEnum.Hashed:
                    if (repoModState.VersionHash.IsMatch(storageModState.VersionHash)) return ModAction.Use;
                    return storageWritable ? ModAction.Update : ModAction.Unusable;
                case StorageModStateEnum.Updating:
                    return storageModState.JobTarget.Hash == repoModState.VersionHash.GetHashString() ? ModAction.Await : ModAction.AbortAndUpdate;
                case StorageModStateEnum.CreatedWithUpdateTarget:
                    if (repoModState.VersionHash.GetHashString() != storageModState.UpdateTarget.Hash) throw new InvalidOperationException();
                    return ModAction.ContinueUpdate;
                case StorageModStateEnum.ErrorUpdate:
                    return ModAction.Error;
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
    }
}