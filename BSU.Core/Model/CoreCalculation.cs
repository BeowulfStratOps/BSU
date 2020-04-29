using System;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    public class CoreCalculation
    {
        internal static ModAction CalculateAction(RepositoryModState repoModState, StorageModState storageModState, bool storageWritable)
        {
            if (storageModState.JobTarget == null)
            {
                if (repoModState.VersionHash.IsMatch(storageModState.VersionHash) && !storageModState.IsUpdating)
                    return ModAction.Use;
                if (!storageWritable) return ModAction.Unusable;
                return storageModState.UpdateTarget != null ? ModAction.ContinueUpdate : ModAction.Update;
            }
            if (storageModState.JobTarget.Hash == repoModState.VersionHash.GetHashString())
                return ModAction.Await;

            return ModAction.AbortAndUpdate;
        }

        // TODO: tuple -> enum
        internal static (bool match, bool requireHash) IsMatch(RepositoryModState repoModState, StorageModState storageModState)
        {
            if (repoModState.IsLoading) return (false, false);

            var isMatch = storageModState.MatchHash != null &&
                          repoModState.MatchHash.IsMatch(storageModState.MatchHash);

            isMatch |= repoModState.VersionHash != null &&
                       storageModState.UpdateTarget?.Hash == repoModState.VersionHash.GetHashString();

            if (!isMatch) return (false, false);
            
            if (storageModState.VersionHash == null) return (false, true);

            return (true, false);
        }
    }
}